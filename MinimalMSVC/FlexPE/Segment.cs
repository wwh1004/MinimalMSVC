using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlexPE;

/// <summary>
/// Similar to System.Memory&lt;byte&gt;
/// </summary>
[DebuggerDisplay("O:{Index} L:{Length} S:{DbgTryToUtf8String()}")]
public readonly unsafe struct Segment {
	readonly byte[]? array;
	readonly byte* pointer;
	readonly int index;
	readonly int length;

	/// <summary>
	/// ONLY USED FOR .ctor <see cref="PEImage(Segment, in LoadOptions, ImageLayout, ImageFormat?)"/> !!!
	/// </summary>
	internal bool IsUnmanaged => pointer is not null;

	/// <summary>
	/// Returns the offset of the base object
	/// </summary>
	public int Index => index;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	/// <summary>
	/// Returns a memory from the current instance.
	/// </summary>
	public Memory<byte> Memory {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => array is not null ? new Memory<byte>(array, index, length) : new PointerToMemory(pointer + index, length).Memory;
	}

	/// <summary>
	/// Returns a span from the current instance.
	/// </summary>
	public Span<byte> Span {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => array is not null ? new Span<byte>(array, index, length) : new Span<byte>(pointer + index, length);
	}
#endif

	#region Memory APIs
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="array"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment(byte[]? array) {
		if (array is null) {
			this = default;
			return; // returns default
		}
		this.array = array;
		pointer = null;
		index = 0;
		length = array.Length;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="array"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment(byte[]? array, int start, int length) {
		if (array is null) {
			if (start != 0 || length != 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			this = default;
			return; // returns default
		}
		if (sizeof(nuint) == 8) {
			// See comment in Span<T>.Slice for how this works.
			if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)array.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
		}
		else {
			if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
				throw new ArgumentOutOfRangeException(nameof(length));
		}

		this.array = array;
		pointer = null;
		index = start;
		this.length = length;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="pointer"></param>
	/// <param name="length"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment(byte* pointer, int length) {
		if (pointer is null) {
			if (length != 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			this = default;
			return; // returns default
		}
		array = null;
		this.pointer = pointer;
		index = 0;
		this.length = length;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="pointer"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment(byte* pointer, int start, int length) {
		if (pointer is null) {
			if (start != 0 || length != 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			this = default;
			return; // returns default
		}
		array = null;
		this.pointer = pointer;
		index = start;
		this.length = length;
	}

	/// <summary>
	/// Defines an implicit conversion of an array to a <see cref="Segment"/>
	/// </summary>
	public static implicit operator Segment(byte[]? array) {
		return new Segment(array);
	}

	/// <summary>
	/// Returns an empty <see cref="Segment"/>
	/// </summary>
	public static Segment Empty => default;

	/// <summary>
	/// The number of items in the memory.
	/// </summary>
	public int Length => length;

	/// <summary>
	/// Returns true if Length is 0.
	/// </summary>
	public bool IsEmpty => length == 0;

	/// <inheritdoc/>
	public override string ToString() {
		return $"byte[{length}]";
	}

	/// <summary>
	/// Forms a slice out of the given memory, beginning at 'start'.
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment Slice(int start) {
		if ((uint)start > (uint)length)
			throw new ArgumentOutOfRangeException(nameof(start));

		return array is not null ? new Segment(array, index + start, length - start) : new Segment(pointer, index + start, length - start);
	}

	/// <summary>
	/// Forms a slice out of the given memory, beginning at 'start', of given length
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <param name="length">The desired length for the slice (exclusive).</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment Slice(int start, int length) {
		if (sizeof(nuint) == 8) {
			// See comment in Span<T>.Slice for how this works.
			if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)this.length)
				throw new ArgumentOutOfRangeException(nameof(length));
		}
		else {
			if ((uint)start > (uint)this.length || (uint)length > (uint)(this.length - start))
				throw new ArgumentOutOfRangeException(nameof(length));
		}

		return array is not null ? new Segment(array, index + start, length) : new Segment(pointer, index + start, length);
	}

	/// <summary>
	/// Copies the contents of the memory into the destination. If the source
	/// and destination overlap, this method behaves as if the original values are in
	/// a temporary location before the destination is overwritten.
	/// </summary>
	/// <param name="destination">The Memory to copy items into.</param>
	/// <exception cref="ArgumentException">Thrown when the destination is shorter than the source.</exception>
	public void CopyTo(Segment destination) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		Span.CopyTo(destination.Span);
#else
		if (destination.Length < length)
			throw new ArgumentException("Destination is too short.");

		if (length == 0)
			return;
		if (destination.array is not null) {
			var source = array is not null ? array : ToArray();
			int sourceIndex = array is not null ? index : 0;
			// Handle overlapping range
			Buffer.BlockCopy(source, sourceIndex, destination.array, destination.index, length);
		}
		else {
			// We also don't know whether destination.pointer is in source.array
			var source = ToArray();
			var p = destination.pointer + destination.index;
			for (int i = 0; i < length; i++)
				p[i] = source[i];
		}
#endif
	}

	/// <summary>
	/// Copies the contents from the memory into a new array.  This heap
	/// allocates, so should generally be avoided, however it is sometimes
	/// necessary to bridge the gap with APIs written in terms of arrays.
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public byte[] ToArray() {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		return Span.ToArray();
#else
		var buffer = new byte[length];
		if (length == 0)
			return buffer;

		if (array is not null)
			Buffer.BlockCopy(array, index, buffer, 0, length);
		else {
			for (int i = 0; i < length; i++)
				buffer[i] = pointer[i];
		}

		return buffer;
#endif
	}

	/// <summary>
	/// Fills the contents of this span with the given value.
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void Fill(byte value) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		Span.Fill(value);
#else
		if (array is not null) {
			int length = this.length + index;
			for (int i = index; i < length; i++)
				array[i] = value;
		}
		else {
			int length = this.length + index;
			for (int i = index; i < length; i++)
				pointer[i] = value;
		}
#endif
	}
	#endregion

	/// <summary>
	/// Forms a slice out of the given memory, beginning at 'start'.
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment TrySlice(int start) {
		if ((uint)start > (uint)length)
			return default;

		return array is not null ? new Segment(array, index + start, length - start) : new Segment(pointer, index + start, length - start);
	}

	/// <summary>
	/// Forms a slice out of the given memory, beginning at 'start', of given length
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <param name="length">The desired length for the slice (exclusive).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Segment TrySlice(int start, int length) {
		if (sizeof(nuint) == 8) {
			// See comment in Span<T>.Slice for how this works.
			if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)this.length)
				return default;
		}
		else {
			if ((uint)start > (uint)this.length || (uint)length > (uint)(this.length - start))
				return default;
		}

		return array is not null ? new Segment(array, index + start, length) : new Segment(pointer, index + start, length);
	}

	/// <summary>
	/// Returns a reference from the current instance.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref byte GetPinnableReference() {
		if (array is not null)
			return ref array[index];
		else
			return ref pointer[index];
	}

	/// <summary>
	/// Reinterpret cast
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T As<T>() where T : unmanaged {
#if NETCOREAPP
		if (array is not null)
			return ref Unsafe.As<byte, T>(ref array[index]);
		else
			return ref *(T*)(pointer + index);
#else
		if (array is not null) {
			fixed (byte* p = array)
				return ref *(T*)(p + index);
		}
		else
			return ref *(T*)(pointer + index);
#endif
	}

	/// <summary>
	/// Try read value at specified offset
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="start"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead<T>(int start, out T value) where T : unmanaged {
		int length = sizeof(T);
		if (sizeof(nuint) == 8) {
			// See comment in Span<T>.Slice for how this works.
			if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)this.length) {
				value = default;
				return false;
			}
		}
		else {
			if ((uint)start > (uint)this.length || (uint)length > (uint)(this.length - start)) {
				value = default;
				return false;
			}
		}

#if NETCOREAPP
		if (array is not null)
			value = Unsafe.ReadUnaligned<T>(ref array[index + start]);
		else
			value = *(T*)(pointer + index + start);
#else
		if (array is not null) {
			fixed (byte* p = array)
				value = *(T*)(p + index + start);
		}
		else
			value = *(T*)(pointer + index + start);
#endif
		return true;
	}

	/// <summary>
	/// Reads value at specified offset
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="start"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Read<T>(int start) where T : unmanaged {
		if (!TryRead(start, out T value))
			throw new InvalidOperationException();
		return value;
	}

	/// <summary>
	/// Reads a null-terminated ascii string
	/// </summary>
	/// <param name="maximumLength"></param>
	/// <returns></returns>
	public string ReadAsciiString(int maximumLength = int.MaxValue) {
		if (IsEmpty)
			return string.Empty;
		int length = Math.Min(this.length, maximumLength);
		var sb = new StringBuilder(Math.Min(length, 0x100));
		length += index;
		if (array is not null) {
			for (int i = index; i < length; i++) {
				if (array[i] == 0)
					break;
				sb.Append((char)array[i]);
			}
		}
		else {
			for (int i = index; i < length; i++) {
				if (pointer[i] == 0)
					break;
				sb.Append((char)pointer[i]);
			}
		}
		return sb.ToString();
	}

	internal int GetAsciiStringLength(int maximumLength = int.MaxValue) {
		if (IsEmpty)
			return 0;
		int length = Math.Min(this.length, maximumLength);
		length += index;
		if (array is not null) {
			for (int i = index; i < length; i++) {
				if (array[i] == 0)
					return i - index;
			}
		}
		else {
			for (int i = index; i < length; i++) {
				if (pointer[i] == 0)
					return i - index;
			}
		}
		return length - index;
	}

	/// <summary>
	/// Moves internal data from <paramref name="sourceIndex"/> to <paramref name="destinationIndex"/>
	/// </summary>
	/// <param name="sourceIndex"></param>
	/// <param name="destinationIndex"></param>
	/// <param name="length">-1 means move maximum available length (length = this.length - Math.Max(sourceIndex, destinationIndex))</param>
	/// <param name="zeroFill">Fills zero with the not overlapping data in source</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void InternalMove(int sourceIndex, int destinationIndex, int length = -1, bool zeroFill = true) {
		if (length == -1)
			length = this.length - Math.Max(sourceIndex, destinationIndex);
		Slice(sourceIndex, length).CopyTo(Slice(destinationIndex, length));
		if (!zeroFill)
			return;
		if (sourceIndex < destinationIndex) {
			int fillLength = Math.Min(destinationIndex - sourceIndex, length);
			Slice(sourceIndex, fillLength).Fill(0);
		}
		else {
			int fillLength = Math.Min(sourceIndex - destinationIndex, length);
			Slice(sourceIndex + length - fillLength, fillLength).Fill(0);
		}
	}

	string DbgTryToUtf8String() {
		const int LIMIT = 0x100;

		if (length == 0)
			return string.Empty;
		var buffer = this[..Math.Min(length, LIMIT)].ToArray();
		for (int i = 0; i < buffer.Length; i++) {
			if (buffer[i] == 0)
				return Encoding.UTF8.GetString(buffer, 0, i) + "...";
		}
		return Encoding.UTF8.GetString(buffer);
	}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	sealed class PointerToMemory : System.Buffers.MemoryManager<byte> {
		readonly byte* pointer;
		readonly int length;

		public PointerToMemory(byte* pointer, int length) {
			this.pointer = pointer;
			this.length = length;
		}

		public override Span<byte> GetSpan() {
			return new Span<byte>(pointer, length);
		}

		public override System.Buffers.MemoryHandle Pin(int elementIndex) {
			return new System.Buffers.MemoryHandle(pointer + elementIndex);
		}

		public override void Unpin() {
		}

		protected override void Dispose(bool disposing) {
		}
	}
#endif
}
