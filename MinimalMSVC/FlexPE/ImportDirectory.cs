using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// List of IMAGE_IMPORT_DESCRIPTOR
/// </summary>
public sealed class ImportDirectory : PESpan {
	#region Fields
	/// <summary/>
	public PESpanList<ImportDescriptor> ImportDescriptors { get; }
	#endregion

	/// <summary>
	/// A high level view of current instance but it is readonly
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public ImportView ImportView => new(this);

	internal ImportDirectory(PEImage peImage) {
		ImportDescriptors = new PESpanList<ImportDescriptor>(() => new ImportDescriptor(peImage), () => GetImportDescriptorCount(peImage), peImage.LoadOptions.MaximumImportModuleCount, peImage.LoadOptions.TruncateIfExceeded);
	}

	internal override void Load(ref LoadContext context) {
		int startIndex = context.Data.Index;
		if (!PreLoad(ref context))
			return;

		ImportDescriptors.Load(ref context);
		// Parse IMAGE_IMPORT_DESCRIPTORs

		SetLoaded(ref context.Data, context.Data.Index - startIndex, !context.Next);
	}

	static int GetImportDescriptorCount(PEImage peImage) {
		// TODO: more reliable implementation
		var importDirectory = peImage.OptionalHeader.ImportDirectory;
		if (importDirectory.LoadResult.State != LoadState.Loaded)
			return 0;
		var importDescriptors = peImage.CreateSegment(importDirectory.VirtualAddress);
		if (importDescriptors.IsEmpty)
			return 0;
		int offset = 0;
		int maximumCount = peImage.LoadOptions.MaximumImportModuleCount + 1;
		for (int i = 0; i < maximumCount; i++) {
			if (!importDescriptors.TryRead(offset + 0xC, out uint name) || name == 0)
				return i;
			// IMAGE_IMPORT_DESCRIPTOR.Name
			if (!importDescriptors.TryRead(offset + 0x10, out uint firstThunk) || firstThunk == 0)
				return i;
			// IMAGE_IMPORT_DESCRIPTOR.FirstThunk
			offset += 0x14;
		}
		return maximumCount;
	}
}

/// <summary>
/// IMAGE_IMPORT_DESCRIPTOR
/// </summary>
[DebuggerDisplay("O:{StartOffset} L:{EndOffset - StartOffset} ST:{LoadResult.State} N:{DisplayName} {GetType().Name}")]
public sealed class ImportDescriptor : PESpan {
	internal readonly PEImage peImage;
	readonly bool is64Bit;

	ref IMAGE_IMPORT_DESCRIPTOR RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_IMPORT_DESCRIPTOR>();
	}

	#region Fields
	/// <summary/>
	public RVA OriginalFirstThunk {
		get => (RVA)RawValue.OriginalFirstThunk;
		set => RawValue.OriginalFirstThunk = (uint)value;
	}

	/// <summary/>
	public uint TimeDateStamp {
		get => RawValue.TimeDateStamp;
		set => RawValue.TimeDateStamp = value;
	}

	/// <summary/>
	public uint ForwarderChain {
		get => RawValue.ForwarderChain;
		set => RawValue.ForwarderChain = value;
	}

	/// <summary/>
	public RVA Name {
		get => (RVA)RawValue.Name;
		set => RawValue.Name = (uint)value;
	}

	/// <summary/>
	public RVA FirstThunk {
		get => (RVA)RawValue.FirstThunk;
		set => RawValue.FirstThunk = (uint)value;
	}
	#endregion

	/// <summary>
	/// Valid entry count
	/// </summary>
	public int Count { get; private set; }

	/// <summary>
	/// Reads string from <see cref="Name"/>
	/// </summary>
	/// <remarks>Memory is read for each call!</remarks>
	public string DisplayName => !IsEmpty ? peImage.ReadAsciiString(Name) : string.Empty;

	/// <summary>
	/// List view from <see cref="OriginalFirstThunk"/>
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public ThunkDataList OriginalThunks => ThunkDataList.Create(peImage, OriginalFirstThunk, Count, peImage.LoadOptions.MaximumImportFunctionCount, is64Bit);

	/// <summary>
	/// List view from <see cref="FirstThunk"/>
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public ThunkDataList Thunks => ThunkDataList.Create(peImage, FirstThunk, Count, peImage.LoadOptions.MaximumImportFunctionCount, is64Bit);

	/// <summary>
	/// A high level view of current instance but it is readonly
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public ImportModuleView ImportModuleView => new(this);

	internal ImportDescriptor(PEImage peImage) {
		this.peImage = peImage;
		is64Bit = peImage.OptionalHeader.IsPE64;
	}

	internal override void Load(ref LoadContext context) {
		Debug.Assert(LoadResult.State == LoadState.NotLoaded, "Not a hot-reloadable class");
		if (!PreLoad(ref context))
			return;
		int length = 0x14;
		if (context.Data.Length < length) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, length, false);
		Count = GetImportEntryCount();
	}

	int GetImportEntryCount() {
		// TODO: more reliable implementation
		var originalFirstThunks = peImage.CreateSegment(OriginalFirstThunk);
		if (originalFirstThunks.IsEmpty)
			return 0;
		int offset = 0;
		int maximumCount = peImage.LoadOptions.MaximumImportFunctionCount + 1;
		for (int i = 0; i < maximumCount; i++) {
			if (is64Bit) {
				if (!originalFirstThunks.TryRead(offset, out ulong originalThunk) || originalThunk == 0)
					return i;
				offset += 8;
			}
			else {
				if (!originalFirstThunks.TryRead(offset, out uint originalThunk) || originalThunk == 0)
					return i;
				offset += 4;
			}
		}
		return maximumCount;
	}
}

/// <summary>
/// IMAGE_THUNK_DATA
/// </summary>
public readonly struct ThunkData {
	readonly ulong value;

	/// <summary>
	/// IMAGE_SNAP_BY_ORDINAL(this)
	/// </summary>
	public bool IsOrdinal => (value & 0x8000000000000000) != 0;

	/// <summary>
	/// IMAGE_THUNK_DATA.Function
	/// </summary>
	public ulong Function => value;

	/// <summary>
	/// IMAGE_THUNK_DATA.Ordinal
	/// </summary>
	/// <remarks>-1 if <see cref="IsOrdinal"/> is <see langword="false"/></remarks>
	public uint Ordinal => IsOrdinal ? (uint)(value & 0x7FFFFFFF) : uint.MaxValue;

	/// <summary>
	/// IMAGE_THUNK_DATA.AddressOfData
	/// </summary>
	/// <remarks>0 if <see cref="IsOrdinal"/> is <see langword="true"/></remarks>
	public RVA AddressOfData => !IsOrdinal ? (RVA)value : 0;

	internal ThunkData(uint value) {
		this.value = (value & 0x80000000) != 0 ? (0x8000000000000000 | (value & 0x7FFFFFFF)) : value;
	}

	internal ThunkData(ulong value) {
		this.value = value;
	}

	/// <inheritdoc/>
	public override string ToString() {
		return value <= uint.MaxValue ? $"0x{value:X8}" : $"0x{value:X16}";
	}
}

/// <summary>
/// Represents an IMAGE_THUNK_DATA list from memory. It is designed to value type for high performance.
/// </summary>
/// <remarks>
/// It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance
/// <para/>
/// PERF: foreach is 4x faster than for with no cache
/// </remarks>
public readonly struct ThunkDataList : IRawData, IEnumerable<ThunkData>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<ThunkData>
#endif
	{
	readonly Segment data;
	readonly int count;
	readonly bool is64Bit;

	/// <summary>
	/// Thunk data count
	/// </summary>
	public int Count => count;

	/// <summary>
	/// Gets a <see cref="ThunkData"/> by <paramref name="index"/>
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public ThunkData this[int index] => index < count ? is64Bit ? new ThunkData(data.Read<ulong>(index * 8)) : new ThunkData(data.Read<uint>(index * 4)) : throw new ArgumentOutOfRangeException(nameof(index));

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

	internal ThunkDataList(Segment data, int count, bool is64Bit) {
		Debug.Assert(data.Length == count * (is64Bit ? 8 : 4));
		this.data = data;
		this.count = count;
		this.is64Bit = is64Bit;
	}

	internal static ThunkDataList Create(PEImage peImage, RVA rva, int expectedCount, int maximumCount, bool is64Bit) {
		if (expectedCount == 0)
			return default;
		if (expectedCount > maximumCount && !peImage.LoadOptions.TruncateIfExceeded)
			return default;
		int count = Math.Min(expectedCount, maximumCount);
		var data = peImage.CreateSegment(rva, count * (is64Bit ? 8 : 4));
		if (data.IsEmpty)
			return default;
		return new ThunkDataList(data, count, is64Bit);
	}

	/// <inheritdoc/>
	public IEnumerator<ThunkData> GetEnumerator() {
		for (int i = 0; i < count; i++)
			yield return this[i];
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}

/// <summary>
/// Represents an import entry
/// </summary>
[DebuggerDisplay("{Name.ReadAsciiString()}: {Function}")]
public readonly record struct ImportEnrty {
	/// <summary>
	/// Index in <see cref="ImportDescriptor.OriginalFirstThunk"/>, <see cref="ImportDescriptor.Name"/> and <see cref="ImportDescriptor.FirstThunk"/>
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Module name, <see cref="ImportDescriptor.Name"/>
	/// </summary>
	public Segment ModuleName { get; }

	/// <summary>
	/// Original thunk data
	/// </summary>
	public ThunkData OriginalThunk { get; }

	/// <summary>
	/// Thunk data
	/// </summary>
	public ThunkData Thunk { get; }

	/// <summary>
	/// Function absolute address
	/// </summary>
	/// <remarks>Extracted from <see cref="Thunk"/>, valid if current PE image is loaded by OS loader</remarks>
	public ulong Function => Thunk.Function;

	/// <summary>
	/// <see cref="OriginalThunk"/>.IsOrdinal
	/// </summary>
	/// <remarks>Extracted from <see cref="OriginalThunk"/></remarks>
	public bool IsOrdinal => OriginalThunk.IsOrdinal;

	/// <summary>
	/// Function ordinal
	/// </summary>
	/// <remarks>Extracted from <see cref="OriginalThunk"/>, -1 if <see cref="ThunkData.IsOrdinal"/> is <see langword="false"/></remarks>
	public uint Ordinal => OriginalThunk.Ordinal;

	/// <summary>
	/// Function hint (see <see cref="ExportEntry.NameIndex"/>)
	/// </summary>
	/// <remarks>Extracted from <see cref="OriginalThunk"/>, can be -1 if <see cref="ThunkData.IsOrdinal"/> is <see langword="true"/></remarks>
	public ushort Hint { get; }

	/// <summary>
	/// Function name
	/// </summary>
	/// <remarks>Extracted from <see cref="OriginalThunk"/>, can be empty if <see cref="ThunkData.IsOrdinal"/> is <see langword="true"/></remarks>
	public Segment Name { get; }

	internal ImportEnrty(int index, in Segment moduleName, ThunkData originalThunk, ThunkData thunk, ushort hint, in Segment name) {
		Index = index;
		ModuleName = moduleName;
		OriginalThunk = originalThunk;
		Thunk = thunk;
		Hint = hint;
		Name = name;
	}
}

/// <summary>
/// A high level view of <see cref="ImportDirectory"/> but it is readonly
/// </summary>
/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
[DebuggerDisplay("ImportModuleView[{Count}]")]
public readonly struct ImportView : IEnumerable<ImportModuleView>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<ImportModuleView>
#endif
	{
	readonly PESpanList<ImportDescriptor> provider;

	/// <summary>
	/// Import module count
	/// </summary>
	public int Count => provider.Count;

	/// <summary>
	/// Gets an import module
	/// </summary>
	/// <param name="index">Index in <see cref="ImportDirectory.ImportDescriptors"/></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public ImportModuleView this[int index] => index < Count ? provider[index].ImportModuleView : throw new ArgumentOutOfRangeException(nameof(index));

	internal ImportView(ImportDirectory provider) {
		this.provider = provider.ImportDescriptors;
	}

	/// <inheritdoc/>
	public IEnumerator<ImportModuleView> GetEnumerator() {
		for (int i = 0; i < Count; i++)
			yield return this[i];
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}

/// <summary>
/// A high level view of <see cref="ImportDescriptor"/> but it is readonly
/// </summary>
/// <remarks>
/// It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance
/// <para/>
/// PERF: foreach is 2x faster than for with no cache
/// </remarks>
[DebuggerDisplay("{Name.ReadAsciiString()}: ImportEntry[{Count}]")]
public readonly struct ImportModuleView : IEnumerable<ImportEnrty>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<ImportEnrty>
#endif
	{
	readonly ImportDescriptor provider;

	/// <summary>
	/// Import module name
	/// </summary>
	public Segment Name => provider.peImage.CreateAsciiStringData(provider.Name);

	/// <summary>
	/// Import function count
	/// </summary>
	public int Count => provider.Count;

	/// <summary>
	/// Gets an import function
	/// </summary>
	/// <param name="index">Index in <see cref="ImportDescriptor.OriginalThunks"/></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <remarks>It is recommended to use 'foreach' to enumerate export entries for higher performance due to iterator has cache</remarks>
	public ImportEnrty this[int index] => index < Count ? GetNoCache(index) : throw new ArgumentOutOfRangeException(nameof(index));

	internal ImportModuleView(ImportDescriptor provider) {
		this.provider = provider;
	}

	ImportEnrty GetNoCache(int index) {
		return Get(index, Name, provider.OriginalThunks, provider.Thunks);
	}

	ImportEnrty Get(int index, in Segment moduleName, in ThunkDataList originalThunks, in ThunkDataList thunks) {
		var originalThunk = originalThunks[index];
		if (originalThunk.IsOrdinal)
			return new ImportEnrty(index, moduleName, originalThunk, thunks[index], ushort.MaxValue, default);
		// Ordinal only
		var nameWithHint = provider.peImage.CreateSegment(originalThunk.AddressOfData);
		if (!nameWithHint.TryRead(0, out ushort hint))
			return new ImportEnrty(index, moduleName, originalThunk, thunks[index], ushort.MaxValue, default);
		var name = nameWithHint[2..];
		name = name[..name.GetAsciiStringLength(provider.peImage.LoadOptions.MaximumStringLength)];
		return new ImportEnrty(index, moduleName, originalThunk, thunks[index], hint, name);
		// Has name
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns></returns>
	public Enumerator GetEnumerator() {
		return new Enumerator(this);
	}

	IEnumerator<ImportEnrty> IEnumerable<ImportEnrty>.GetEnumerator() {
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	/// <summary/>
	public struct Enumerator : IEnumerator<ImportEnrty>, IEnumerator {
		readonly ImportModuleView view;
		readonly Segment moduleName;
		readonly ThunkDataList originalThunks;
		readonly ThunkDataList thunks;
		int index;
		ImportEnrty current;

		internal Enumerator(in ImportModuleView view) {
			if (view.Count == 0) {
				this = default;
				return;
			}
			this.view = view;
			moduleName = view.provider.peImage.CreateAsciiStringData(view.provider.Name);
			originalThunks = view.provider.OriginalThunks;
			thunks = view.provider.Thunks;
			index = 0;
			current = default;
		}

		/// <inheritdoc/>
		public void Dispose() {
		}

		/// <inheritdoc/>
		public bool MoveNext() {
			if ((uint)index < (uint)view.Count) {
				current = view.Get(index, moduleName, originalThunks, thunks);
				index++;
				return true;
			}
			return MoveNextRare();
		}

		bool MoveNextRare() {
			index = view.Count + 1;
			current = default;
			return false;
		}

		/// <inheritdoc/>
		public ImportEnrty Current => current;

		/// <inheritdoc/>
		object IEnumerator.Current => current;

		/// <inheritdoc/>
		void IEnumerator.Reset() {
			index = 0;
			current = default;
		}
	}
}
