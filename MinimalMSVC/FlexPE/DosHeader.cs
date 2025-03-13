using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_DOS_HEADER
/// </summary>
public sealed class DosHeader : PESpan {
	readonly PEImage peImage;

	ref IMAGE_DOS_HEADER RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_DOS_HEADER>();
	}

	#region Fields
	/// <summary/>
	public ushort Magic {
		get => RawValue.e_magic;
		set => RawValue.e_magic = value;
	}

	/// <summary/>
	public uint Lfanew {
		get => RawValue.e_lfanew;
		set => RawValue.e_lfanew = value;
	}
	#endregion

	internal DosHeader(PEImage peImage) {
		this.peImage = peImage;
	}

	internal override void Load(ref LoadContext context) {
		if (!PreLoad(ref context))
			return;
		int length = 0x40;
		if (context.Data.Length < length) {
			SetError(ref context.Next);
			return;
		}
		if (peImage.LoadOptions.CheckSignature && (!context.Data.TryRead(0, out ushort signature) || signature != IMAGE_DOS_SIGNATURE)) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, length, false);
	}
}
