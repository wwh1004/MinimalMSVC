using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_NT_HEADERS
/// </summary>
public sealed class NtHeaders : PESpan {
	readonly PEImage peImage;

	ref IMAGE_NT_HEADERS32 RawValue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_NT_HEADERS32>();
	}

	#region Fields
	/// <summary/>
	public uint Signature {
		get => RawValue.Signature;
		set => RawValue.Signature = value;
	}

	/// <summary/>
	public FileHeader FileHeader { get; }

	/// <summary/>
	public OptionalHeader OptionalHeader { get; }
	#endregion

	internal NtHeaders(PEImage peImage) {
		this.peImage = peImage;
		FileHeader = new FileHeader();
		OptionalHeader = new OptionalHeader(peImage);
	}

	internal override void Load(ref LoadContext context) {
		var startData = context.Data;
		var oldData = RawData;
		LoadSelf(ref context);
		bool selfLoaded = context.Next;
		// Parse IMAGE_NT_HEADERS fields

		FileHeader.Load(ref context);
		// Parse IMAGE_FILE_HEADER

		OptionalHeader.Load(ref context);
		// Parse IMAGE_OPTIONAL_HEADER

		if (selfLoaded)
			SetLoaded(ref context.Data, startData, oldData, context.Data.Index - startData.Index, !context.Next);
	}

	void LoadSelf(ref LoadContext context) {
		if (!PreLoad(ref context))
			return;
		if (context.Data.Length < 4) {
			SetError(ref context.Next);
			return;
		}
		if (peImage.LoadOptions.CheckSignature && (!context.Data.TryRead(0, out uint signature) || signature != IMAGE_NT_SIGNATURE)) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, 4, true);
	}
}
