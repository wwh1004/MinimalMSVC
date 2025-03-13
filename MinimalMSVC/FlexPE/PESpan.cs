using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FlexPE;

/// <summary>
/// Represents a span of <see cref="PEImage"/>
/// </summary>
[DebuggerDisplay("O:{StartOffset} L:{EndOffset - StartOffset} ST:{LoadResult.State} {GetType().Name}")]
public abstract class PESpan : IRawData {
	Segment data;

	/// <inheritdoc/>
	public Segment RawData => data;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	/// <inheritdoc/>
	public Span<byte> RawSpan => data.Span;

	/// <inheritdoc/>
	public Memory<byte> RawMemory => data.Memory;
#endif

	/// <summary>
	/// Whether <see cref="RawData"/> is empty
	/// </summary>
	public bool IsEmpty => data.IsEmpty;

	/// <inheritdoc/>
	public FileOffset StartOffset => (FileOffset)data.Index;

	/// <inheritdoc/>
	public FileOffset EndOffset => (FileOffset)(data.Index + data.Length);

	/// <summary>
	/// Last loading result
	/// </summary>
	public LoadResult LoadResult { get; private set; }

	internal PESpan() {
	}

	/// <summary>
	/// Entry point of loading 
	/// </summary>
	/// <param name="context"></param>
	internal abstract void Load(ref LoadContext context);

	internal /*protected*/ bool PreLoad(ref LoadContext context) {
		if (!context.Next) {
			SetNotExist();
			return false;
		}
		if (context.SetStateOnce is LoadState state) {
			context.SetStateOnce = null;
			if (state == LoadState.Error) {
				SetError(ref context.Next);
				return false;
			}
			Debug.Assert(false);
		}
		return true;
	}

	/// <summary/>
	protected void SetError(ref bool next) {
		var diff = IsEmpty ? LoadDiff.None : LoadDiff.Deleted;
		LoadResult = new LoadResult(LoadState.Error, diff);
		data = default;
		next = false;
	}

	/// <summary/>
	protected void SetNotExist() {
		var diff = IsEmpty ? LoadDiff.None : LoadDiff.Deleted;
		LoadResult = new LoadResult(LoadState.NotExist, diff);
		data = default;
	}

	/// <summary/>
	protected void SetLoaded(ref Segment data, int length, bool partlyLoaded) {
		var startData = data;
		var oldData = this.data;
		SetLoaded(ref data, startData, oldData, length, partlyLoaded);
	}

	/// <summary/>
	protected void SetLoaded(ref Segment data, in Segment startData, in Segment oldData, int length, bool partlyLoaded) {
		if (data.IsEmpty)
			throw new InvalidOperationException($"Please call {nameof(SetError)} or {nameof(SetNotExist)}");

		LoadDiff diff;
		if (data.Index != oldData.Index || length != oldData.Length) {
			diff = IsEmpty ? LoadDiff.Added : LoadDiff.Moved;
			this.data = startData[..length];
		}
		else
			diff = LoadDiff.None;
		data = startData[length..];
		LoadResult = new LoadResult(partlyLoaded ? LoadState.PartlyLoaded : LoadState.Loaded, diff);
	}

	/// <summary/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected ref T AsThrowing<T>() where T : unmanaged {
		if (IsEmpty)
			throw new InvalidOperationException();
		return ref data.As<T>();
	}
}

/// <summary>
/// Represents the list of <see cref="PESpan"/>
/// </summary>
/// <typeparam name="T"></typeparam>
[DebuggerDisplay("O:{StartOffset} L:{EndOffset - StartOffset} ST:{LoadResult.State} {typeof(T).Name,nq}[{Count}]")]
public sealed class PESpanList<T> : PESpan, IList<T>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<T>
#endif
	where T : PESpan {
	readonly List<T> list;
	readonly Func<T> factory;
	readonly Func<int> getExpectedCount;
	readonly int maximumCount;
	readonly bool truncateIfExceeded;

	#region List APIs
	/// <inheritdoc/>
	public T this[int index] => list[index];

	/// <inheritdoc/>
	public int Count => list.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => true;

	/// <inheritdoc/>
	public bool Contains(T item) {
		return list.Contains(item);
	}

	/// <inheritdoc/>
	public void CopyTo(T[] array, int arrayIndex) {
		list.CopyTo(array, arrayIndex);
	}

	/// <summary/>
	public List<T>.Enumerator GetEnumerator() {
		return list.GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator() {
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	/// <inheritdoc/>
	public int IndexOf(T item) {
		return list.IndexOf(item);
	}

	T IList<T>.this[int index] {
		get => list[index];
		set => throw new NotSupportedException();
	}

	void ICollection<T>.Add(T item) {
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear() {
		throw new NotSupportedException();
	}

	void IList<T>.Insert(int index, T item) {
		throw new NotSupportedException();
	}

	bool ICollection<T>.Remove(T item) {
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index) {
		throw new NotSupportedException();
	}
	#endregion

	internal PESpanList(Func<T> factory, Func<int> getExpectedCount, int maximumCount, bool truncateIfExceeded) {
		list = new List<T>();
		this.factory = factory;
		this.getExpectedCount = getExpectedCount;
		this.maximumCount = maximumCount;
		this.truncateIfExceeded = truncateIfExceeded;
	}

	internal override void Load(ref LoadContext context) {
		list.Clear();
		var startData = context.Data;
		var oldData = RawData;
		if (!PreLoad(ref context))
			return;
		int expectedCount = getExpectedCount();
		if (expectedCount == 0) {
			SetNotExist();
			return;
		}
		if (expectedCount > maximumCount && !truncateIfExceeded) {
			SetError(ref context.Next);
			return;
		}
		int count = Math.Min(expectedCount, maximumCount);
		for (int i = 0; i < count; i++) {
			var span = factory();
			span.Load(ref context);
			if (!context.Next)
				break;
			list.Add(span);
		}
		if (list.Count == 0) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, startData, oldData, context.Data.Index - startData.Index, list.Count != expectedCount);
	}
}

/// <summary>
/// Represents an unmanaged value list from memory. It is designed to value type for high performance.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
/// It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance
/// <para/>
/// PERF: foreach is 7.5x faster than for with no cache
/// </remarks>
public readonly struct PEUnmanagedList<T> : IRawData, IEnumerable<T>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<T>
#endif
	where T : unmanaged {
	static readonly unsafe int ElementSize = sizeof(T);

	readonly Segment data;
	readonly int count;

	/// <inheritdoc/>
	public int Count => count;

	/// <summary/>
	public ref T this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			if ((uint)index >= (uint)Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ref data.Slice(index * ElementSize, ElementSize).As<T>();
		}
	}

	#region IRawData APIs
	/// <inheritdoc/>
	public Segment RawData => data;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	/// <summary/>
	public Span<byte> RawSpan => data.Span;

	/// <summary/>
	public Memory<byte> RawMemory => data.Memory;
#endif

	/// <inheritdoc/>
	public bool IsEmpty => data.IsEmpty;

	/// <inheritdoc/>
	public FileOffset StartOffset => (FileOffset)data.Index;

	/// <inheritdoc/>
	public FileOffset EndOffset => (FileOffset)(data.Index + data.Length);
	#endregion

	#region IEnumerable APIs
	IEnumerator<T> IEnumerable<T>.GetEnumerator() {
		for (int i = 0; i < Count; i++)
			yield return this[i];
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable<T>)this).GetEnumerator();
	}

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	T IReadOnlyList<T>.this[int index] => this[index];
#endif
	#endregion

	internal PEUnmanagedList(Segment data, int count) {
		Debug.Assert(data.Length == count * ElementSize);
		this.data = data;
		this.count = count;
	}

	internal static PEUnmanagedList<T> Create(PEImage peImage, RVA rva, int expectedCount, int maximumCount) {
		if (expectedCount == 0)
			return default;
		if (expectedCount > maximumCount && !peImage.LoadOptions.TruncateIfExceeded)
			return default;
		int count = Math.Min(expectedCount, maximumCount);
		var data = peImage.CreateSegment(rva, count * ElementSize);
		if (data.IsEmpty)
			return default;
		return new PEUnmanagedList<T>(data, count);
	}

	/// <summary/>
	public Enumerator GetEnumerator() {
		return new Enumerator(this);
	}

	/// <summary>Enumerates the elements of a <see cref="PEUnmanagedList{T}"/>.</summary>
	public struct Enumerator {
		readonly PEUnmanagedList<T> list;
		int index;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Enumerator(in PEUnmanagedList<T> list) {
			this.list = list;
			index = -1;
		}

		/// <summary>Advances the enumerator to the next element of the span.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext() {
			int index = this.index + 1;
			if (index < list.count) {
				this.index = index;
				return true;
			}
			return false;
		}

		/// <summary>Gets the element at the current position of the enumerator.</summary>
		public ref T Current {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref list.data.Slice(index * ElementSize, ElementSize).As<T>();
		}
	}
}

//public class PEUnmanagedList<T> : PESpan, IList<T>
//#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
//	, IReadOnlyList<T>
//#endif
//	where T : unmanaged {
//	static readonly int ElementSize =
//#if NETCOREAPP
//		Unsafe.SizeOf<T>();
//#else
//		System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
//#endif

//	readonly Func<int> getExpectedCount;
//	readonly int maximumCount;

//	#region List APIs
//	public ref T this[int index] {
//		[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		get {
//			if ((uint)index >= (uint)Count)
//				throw new ArgumentOutOfRangeException(nameof(index));
//			return ref RawData.Slice(index * ElementSize, ElementSize).As<T>();
//		}
//	}

//	public int Count { get; private set; }

//	public bool IsReadOnly => true;

//	public void CopyTo(T[] array, int arrayIndex) {
//		if (array is null)
//			throw new ArgumentNullException(nameof(array));
//		if ((uint)arrayIndex >= (uint)array.Length)
//			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
//		if (array.Length - arrayIndex < Count)
//			throw new ArgumentException("Not enough space");

//		if (Count == 0)
//			return;
//#if NETCOREAPP
//		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref array[0]), ref RawData.GetPinnableReference(), (uint)(Count * ElementSize));
//#else
//		for (int i = 0; i < Count; i++)
//			array[arrayIndex + i] = this[i];
//#endif
//	}

//	public IEnumerator<T> GetEnumerator() {
//		for (int i = 0; i < Count; i++)
//			yield return this[i];
//	}

//	IEnumerator IEnumerable.GetEnumerator() {
//		return GetEnumerator();
//	}

//	T IList<T>.this[int index] {
//		get => this[index];
//		set => this[index] = value;
//	}

//#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
//	T IReadOnlyList<T>.this[int index] => this[index];
//#endif

//	void ICollection<T>.Add(T item) {
//		throw new NotSupportedException();
//	}

//	void ICollection<T>.Clear() {
//		throw new NotSupportedException();
//	}

//	bool ICollection<T>.Contains(T item) {
//		throw new NotSupportedException();
//	}

//	int IList<T>.IndexOf(T item) {
//		throw new NotSupportedException();
//	}

//	void IList<T>.Insert(int index, T item) {
//		throw new NotSupportedException();
//	}

//	bool ICollection<T>.Remove(T item) {
//		throw new NotSupportedException();
//	}

//	void IList<T>.RemoveAt(int index) {
//		throw new NotSupportedException();
//	}
//	#endregion

//	internal PEUnmanagedList(Func<int> getExpectedCount, int maximumCount) {
//		this.getExpectedCount = getExpectedCount;
//		this.maximumCount = maximumCount;
//	}

//	internal override void Load(ref LoadContext context) {
//		Count = 0;
//		if (!PreLoad(ref context))
//			return;
//		int expectedCount = getExpectedCount();
//		if (expectedCount == 0) {
//			SetNotExist();
//			return;
//		}
//		if (expectedCount > maximumCount) {
//			SetError(ref context.Next);
//			return;
//		}
//		Count = Math.Min(expectedCount, context.Data.Length / ElementSize);
//		SetLoaded(ref context.Data, Count * ElementSize, Count == expectedCount);
//	}

//	internal static PEUnmanagedList<T> Create(PEImage peImage, RVA rva, Func<int> getExpectedCount, int maximumCount) {
//		var list = new PEUnmanagedList<T>(getExpectedCount, maximumCount);
//		var context = new LoadContext {
//			Data = peImage.CreateSegment(rva),
//			Next = true
//		};
//		list.Load(ref context);
//		return list;
//	}
//}
