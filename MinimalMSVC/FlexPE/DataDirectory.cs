using System.Diagnostics;
using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_DATA_DIRECTORY (No hot-reload support)
/// </summary>
[DebuggerDisplay("RVA:{VirtualAddress} Size:{Size} State:{LoadResult.State}")]
public sealed class DataDirectory : PESpan {
	ref IMAGE_DATA_DIRECTORY RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_DATA_DIRECTORY>();
	}

	#region Fields
	/// <summary/>
	public RVA VirtualAddress {
		get => (RVA)RawValue.VirtualAddress;
		set => RawValue.VirtualAddress = (uint)value;
	}

	/// <summary/>
	public uint Size {
		get => RawValue.Size;
		set => RawValue.Size = value;
	}
	#endregion

	internal DataDirectory() {
	}

	internal override void Load(ref LoadContext context) {
		Debug.Assert(LoadResult.State == LoadState.NotLoaded, "Not a hot-reloadable class");
		if (!PreLoad(ref context))
			return;
		int length = 8;
		if (context.Data.Length < length) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, length, false);
	}
}
