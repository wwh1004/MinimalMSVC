using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_SECTION_HEADER (No hot-reload support)
/// </summary>
[DebuggerDisplay("RVA:{VirtualAddress} VS:{VirtualSize} FO:{PointerToRawData} FS:{SizeOfRawData} {DisplayName}")]
public sealed class SectionHeader : PESpan {
	readonly PEImage peImage;

	ref IMAGE_SECTION_HEADER RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_SECTION_HEADER>();
	}

	#region Fields
	/// <summary/>
	public Segment Name => !IsEmpty ? RawData[..IMAGE_SIZEOF_SHORT_NAME] : throw new InvalidOperationException();

	/// <summary/>
	public uint VirtualSize {
		get => RawValue.VirtualSize;
		set => RawValue.VirtualSize = value;
	}

	/// <summary/>
	public RVA VirtualAddress {
		get => (RVA)RawValue.VirtualAddress;
		set => RawValue.VirtualAddress = (uint)value;
	}

	/// <summary/>
	public uint SizeOfRawData {
		get => RawValue.SizeOfRawData;
		set => RawValue.SizeOfRawData = value;
	}

	/// <summary/>
	public FileOffset PointerToRawData {
		get => (FileOffset)RawValue.PointerToRawData;
		set => RawValue.PointerToRawData = (uint)value;
	}

	/// <summary/>
	public uint PointerToRelocations {
		get => RawValue.PointerToRelocations;
		set => RawValue.PointerToRelocations = value;
	}

	/// <summary/>
	public uint PointerToLinenumbers {
		get => RawValue.PointerToLinenumbers;
		set => RawValue.PointerToLinenumbers = value;
	}

	/// <summary/>
	public ushort NumberOfRelocations {
		get => RawValue.NumberOfRelocations;
		set => RawValue.NumberOfRelocations = value;
	}

	/// <summary/>
	public ushort NumberOfLinenumbers {
		get => RawValue.NumberOfLinenumbers;
		set => RawValue.NumberOfLinenumbers = value;
	}

	/// <summary/>
	public uint Characteristics {
		get => RawValue.Characteristics;
		set => RawValue.Characteristics = value;
	}
	#endregion

	/// <summary>
	/// Reads string from <see cref="Name"/>
	/// </summary>
	/// <remarks>Memory is read for each call!</remarks>
	public string DisplayName => !IsEmpty ? peImage.ReadAsciiString(RawData[..IMAGE_SIZEOF_SHORT_NAME]) : string.Empty;

	internal SectionHeader(PEImage peImage) {
		this.peImage = peImage;
	}

	internal override void Load(ref LoadContext context) {
		Debug.Assert(LoadResult.State == LoadState.NotLoaded, "Not a hot-reloadable class");
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
