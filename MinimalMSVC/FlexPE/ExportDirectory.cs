using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_EXPORT_DIRECTORY
/// </summary>
[DebuggerDisplay("O:{StartOffset} L:{EndOffset - StartOffset} ST:{LoadResult.State} N:{DisplayName} {GetType().Name}")]
public sealed class ExportDirectory : PESpan {
	internal readonly PEImage peImage;

	ref IMAGE_EXPORT_DIRECTORY RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_EXPORT_DIRECTORY>();
	}

	#region Fields
	/// <summary/>
	public uint Characteristics {
		get => RawValue.Characteristics;
		set => RawValue.Characteristics = value;
	}

	/// <summary/>
	public uint TimeDateStamp {
		get => RawValue.TimeDateStamp;
		set => RawValue.TimeDateStamp = value;
	}

	/// <summary/>
	public ushort MajorVersion {
		get => RawValue.MajorVersion;
		set => RawValue.MajorVersion = value;
	}

	/// <summary/>
	public ushort MinorVersion {
		get => RawValue.MinorVersion;
		set => RawValue.MinorVersion = value;
	}

	/// <summary/>
	public RVA Name {
		get => (RVA)RawValue.Name;
		set => RawValue.Name = (uint)value;
	}

	/// <summary/>
	public uint Base {
		get => RawValue.Base;
		set => RawValue.Base = value;
	}

	/// <summary/>
	public uint NumberOfFunctions {
		get => RawValue.NumberOfFunctions;
		set => RawValue.NumberOfFunctions = value;
	}

	/// <summary/>
	public uint NumberOfNames {
		get => RawValue.NumberOfNames;
		set => RawValue.NumberOfNames = value;
	}

	/// <summary/>
	public RVA AddressOfFunctions {
		get => (RVA)RawValue.AddressOfFunctions;
		set => RawValue.AddressOfFunctions = (uint)value;
	}

	/// <summary/>
	public RVA AddressOfNames {
		get => (RVA)RawValue.AddressOfNames;
		set => RawValue.AddressOfNames = (uint)value;
	}

	/// <summary/>
	public RVA AddressOfNameOrdinals {
		get => (RVA)RawValue.AddressOfNameOrdinals;
		set => RawValue.AddressOfNameOrdinals = (uint)value;
	}
	#endregion

	/// <summary>
	/// Reads string from <see cref="Name"/>
	/// </summary>
	/// <remarks>Memory is read for each call!</remarks>
	public string DisplayName => !IsEmpty ? peImage.ReadAsciiString(Name) : string.Empty;

	/// <summary>
	/// List view from <see cref="AddressOfFunctions"/>
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public PEUnmanagedList<RVA> Functions => PEUnmanagedList<RVA>.Create(peImage, AddressOfFunctions, (int)NumberOfFunctions, peImage.LoadOptions.MaximumNumberOfFunctions);

	/// <summary>
	/// List view from <see cref="AddressOfNames"/>
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public PEUnmanagedList<RVA> Names => PEUnmanagedList<RVA>.Create(peImage, AddressOfNames, (int)NumberOfNames, peImage.LoadOptions.MaximumNumberOfFunctions);

	/// <summary>
	/// List view from <see cref="AddressOfNameOrdinals"/>
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public PEUnmanagedList<ushort> NameOrdinals => PEUnmanagedList<ushort>.Create(peImage, AddressOfNameOrdinals, (int)NumberOfNames, peImage.LoadOptions.MaximumNumberOfFunctions);

	/// <summary>
	/// A high level view of current instance but it is readonly
	/// </summary>
	/// <remarks>It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance</remarks>
	public ExportView ExportView => new(this, (int)(NumberOfFunctions - NumberOfNames));

	internal ExportDirectory(PEImage peImage) {
		this.peImage = peImage;
	}

	internal override void Load(ref LoadContext context) {
		if (!PreLoad(ref context))
			return;
		int length = 0x28;
		if (context.Data.Length < length) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, length, false);
	}
}

/// <summary>
/// Represents an export entry
/// </summary>
[DebuggerDisplay("{Name.ReadAsciiString()}: {Function}")]
public readonly record struct ExportEntry {
	/// <summary>
	/// Index in <see cref="ExportDirectory.AddressOfFunctions"/>
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Index of <see cref="Name"/> in <see cref="ExportDirectory.Names"/> and <see cref="ExportDirectory.NameOrdinals"/>, returns -1 if no <see cref="Name"/>
	/// </summary>
	/// <remarks>In most cases it is equal to <see cref="Index"/>. But it can also be not equal to <see cref="Index"/> when value in <see cref="ExportDirectory.NameOrdinals"/> isn't equal to its index.</remarks>
	public int NameIndex { get; }

	/// <summary>
	/// Export ordinal
	/// </summary>
	public uint Ordinal { get; }

	/// <summary>
	/// Function RVA
	/// </summary>
	/// <remarks>0 if entry is dummy and used for placeholder, it is not a valid export entry</remarks>
	public RVA Function { get; }

	/// <summary>
	/// Function name
	/// </summary>
	public Segment Name { get; }

	internal ExportEntry(int index, int nameIndex, uint ordinal, RVA function, Segment name) {
		Index = index;
		NameIndex = nameIndex;
		Ordinal = ordinal;
		Function = function;
		Name = name;
	}
}

/// <summary>
/// A high level view of <see cref="ExportDirectory"/> but it is readonly
/// </summary>
/// <remarks>
/// It is recommended to cache the return value if you will use it more than once although it costs very little to create the current instance
/// <para/>
/// PERF: foreach is 2.5x faster than for with no cache
/// </remarks>
[DebuggerDisplay("{Name.ReadAsciiString()}: ExportEntry[{Count}]")]
public readonly struct ExportView : IEnumerable<ExportEntry>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
	, IReadOnlyList<ExportEntry>
#endif
	{
	readonly ExportDirectory provider;
	readonly int ordinalOnlyCount;
	readonly int count;

	/// <summary>
	/// Export module name
	/// </summary>
	public Segment Name => provider.peImage.CreateAsciiStringData(provider.Name);

	/// <summary>
	/// Export function count
	/// </summary>
	public int Count => count;

	/// <summary>
	/// Gets an export entry
	/// </summary>
	/// <param name="index">Index in <see cref="ExportDirectory.Functions"/></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <remarks>It is recommended to use 'foreach' to enumerate export entries for higher performance due to iterator has cache</remarks>
	public ExportEntry this[int index] => index < count ? GetNoCache(index) : throw new ArgumentOutOfRangeException(nameof(index));

	internal ExportView(ExportDirectory provider, int ordinalOnlyCount) {
		this.provider = provider;
		this.ordinalOnlyCount = ordinalOnlyCount;
		count = (int)Math.Min(provider.NumberOfFunctions, provider.peImage.LoadOptions.MaximumNumberOfFunctions);
	}

	ExportEntry GetNoCache(int index) {
		return Get(index, provider.Functions, provider.Names, provider.NameOrdinals);
	}

	ExportEntry Get(int index, in PEUnmanagedList<RVA> functions, in PEUnmanagedList<RVA> names, in PEUnmanagedList<ushort> nameOrdinals) {
		int nameIndex = LookupNameByIndex(nameOrdinals, index, ordinalOnlyCount);
		if (nameIndex == -1)
			return new ExportEntry(index, nameIndex, (uint)index + provider.Base, functions[index], default);
		// Ordinal only
		return new ExportEntry(index, nameIndex, (uint)index + provider.Base, functions[index], provider.peImage.CreateAsciiStringData(names[nameIndex]));
		// Has name
	}

	static int LookupNameByIndex(in PEUnmanagedList<ushort> nameOrdinals, int index, int ordinalOnlyCount) {
		if (CheckNameOrdinal(nameOrdinals, index, index))
			return index;
		// No ordinal only export entry
		if (ordinalOnlyCount > 0 && index - ordinalOnlyCount >= 0 && CheckNameOrdinal(nameOrdinals, index, index - ordinalOnlyCount))
			return index - ordinalOnlyCount;
		// Has ordinal only export entry, AllEntries=OrdinalOnlyEntries+NamedEntries
		int nameIndex = 0;
		foreach (ushort nameOrdinal in nameOrdinals) {
			if (index == nameOrdinal)
				return nameIndex;
			nameIndex++;
		}
		// Not compiler generated, extremely slow path
		return -1;
	}

	static bool CheckNameOrdinal(in PEUnmanagedList<ushort> nameOrdinals, int index, int nameIndex) {
		if (nameIndex >= nameOrdinals.Count)
			return false;
		return index == nameOrdinals[nameIndex];
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns></returns>
	public Enumerator GetEnumerator() {
		return new Enumerator(this);
	}

	/// <inheritdoc/>
	IEnumerator<ExportEntry> IEnumerable<ExportEntry>.GetEnumerator() {
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	/// <summary/>
	public struct Enumerator : IEnumerator<ExportEntry>, IEnumerator {
		readonly ExportView view;
		readonly PEUnmanagedList<RVA> functions;
		readonly PEUnmanagedList<RVA> names;
		readonly PEUnmanagedList<ushort> nameOrdinals;
		int index;
		ExportEntry current;

		internal Enumerator(in ExportView view) {
			if (view.count == 0) {
				this = default;
				return;
			}
			this.view = view;
			functions = view.provider.Functions;
			names = view.provider.Names;
			nameOrdinals = view.provider.NameOrdinals;
			index = 0;
			current = default;
		}

		/// <inheritdoc/>
		public void Dispose() {
		}

		/// <inheritdoc/>
		public bool MoveNext() {
			if ((uint)index < (uint)view.count) {
				current = view.Get(index, functions, names, nameOrdinals);
				index++;
				return true;
			}
			return MoveNextRare();
		}

		bool MoveNextRare() {
			index = view.count + 1;
			current = default;
			return false;
		}

		/// <inheritdoc/>
		public ExportEntry Current => current;

		/// <inheritdoc/>
		object IEnumerator.Current => current;

		/// <inheritdoc/>
		void IEnumerator.Reset() {
			index = 0;
			current = default;
		}
	}
}
