using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_FILE_HEADER
/// </summary>
public sealed class FileHeader : PESpan {
	ref IMAGE_FILE_HEADER RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_FILE_HEADER>();
	}

	#region Fields
	/// <summary/>
	public Machine Machine {
		get => (Machine)RawValue.Machine;
		set => RawValue.Machine = (ushort)value;
	}

	/// <summary/>
	public ushort NumberOfSections {
		get => RawValue.NumberOfSections;
		set => RawValue.NumberOfSections = value;
	}

	/// <summary/>
	public uint TimeDateStamp {
		get => RawValue.TimeDateStamp;
		set => RawValue.TimeDateStamp = value;
	}

	/// <summary/>
	public FileOffset PointerToSymbolTable {
		get => (FileOffset)RawValue.PointerToSymbolTable;
		set => RawValue.PointerToSymbolTable = (uint)value;
	}

	/// <summary/>
	public uint NumberOfSymbols {
		get => RawValue.NumberOfSymbols;
		set => RawValue.NumberOfSymbols = value;
	}

	/// <summary/>
	public ushort SizeOfOptionalHeader {
		get => RawValue.SizeOfOptionalHeader;
		set => RawValue.SizeOfOptionalHeader = value;
	}

	/// <summary/>
	public Characteristics Characteristics {
		get => (Characteristics)RawValue.Characteristics;
		set => RawValue.Characteristics = (ushort)value;
	}
	#endregion

	internal FileHeader() {
	}

	internal override void Load(ref LoadContext context) {
		if (!PreLoad(ref context))
			return;
		int length = 0x14;
		if (context.Data.Length < length) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, length, false);
	}
}
