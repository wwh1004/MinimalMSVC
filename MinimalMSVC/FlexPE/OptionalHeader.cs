using System;
using System.Runtime.CompilerServices;
using static FlexPE.NativeMethods;

namespace FlexPE;

/// <summary>
/// IMAGE_OPTIONAL_HEADER
/// </summary>
public sealed class OptionalHeader : PESpan {
	static readonly DataDirectory notExistDataDirectory = CreateNotExistDataDirectory();

	readonly PEImage peImage;
	bool isPE32;

	ref IMAGE_OPTIONAL_HEADER32 RawValue32 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_OPTIONAL_HEADER32>();
	}

	ref IMAGE_OPTIONAL_HEADER64 RawValue64 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsThrowing<IMAGE_OPTIONAL_HEADER64>();
	}

	#region Fields
	/// <summary/>
	public ushort Magic {
		get => isPE32 ? RawValue32.Magic : RawValue64.Magic;
		set {
			if (isPE32)
				RawValue32.Magic = value;
			else
				RawValue64.Magic = value;
		}
	}

	/// <summary/>
	public byte MajorLinkerVersion {
		get => isPE32 ? RawValue32.MajorLinkerVersion : RawValue64.MajorLinkerVersion;
		set {
			if (isPE32)
				RawValue32.MajorLinkerVersion = value;
			else
				RawValue64.MajorLinkerVersion = value;
		}
	}

	/// <summary/>
	public byte MinorLinkerVersion {
		get => isPE32 ? RawValue32.MinorLinkerVersion : RawValue64.MinorLinkerVersion;
		set {
			if (isPE32)
				RawValue32.MinorLinkerVersion = value;
			else
				RawValue64.MinorLinkerVersion = value;
		}
	}

	/// <summary/>
	public uint SizeOfCode {
		get => isPE32 ? RawValue32.SizeOfCode : RawValue64.SizeOfCode;
		set {
			if (isPE32)
				RawValue32.SizeOfCode = value;
			else
				RawValue64.SizeOfCode = value;
		}
	}

	/// <summary/>
	public uint SizeOfInitializedData {
		get => isPE32 ? RawValue32.SizeOfInitializedData : RawValue64.SizeOfInitializedData;
		set {
			if (isPE32)
				RawValue32.SizeOfInitializedData = value;
			else
				RawValue64.SizeOfInitializedData = value;
		}
	}

	/// <summary/>
	public uint SizeOfUninitializedData {
		get => isPE32 ? RawValue32.SizeOfUninitializedData : RawValue64.SizeOfUninitializedData;
		set {
			if (isPE32)
				RawValue32.SizeOfUninitializedData = value;
			else
				RawValue64.SizeOfUninitializedData = value;
		}
	}

	/// <summary/>
	public uint AddressOfEntryPoint {
		get => isPE32 ? RawValue32.AddressOfEntryPoint : RawValue64.AddressOfEntryPoint;
		set {
			if (isPE32)
				RawValue32.AddressOfEntryPoint = value;
			else
				RawValue64.AddressOfEntryPoint = value;
		}
	}

	/// <summary/>
	public uint BaseOfCode {
		get => isPE32 ? RawValue32.BaseOfCode : RawValue64.BaseOfCode;
		set {
			if (isPE32)
				RawValue32.BaseOfCode = value;
			else
				RawValue64.BaseOfCode = value;
		}
	}

	/// <summary/>
	public uint BaseOfData {
		get => isPE32 ? RawValue32.BaseOfData : throw new InvalidOperationException();
		set {
			if (isPE32)
				RawValue32.BaseOfData = value;
			else
				throw new InvalidOperationException();
		}
	}

	/// <summary/>
	public ulong ImageBase {
		get => isPE32 ? RawValue32.ImageBase : RawValue64.ImageBase;
		set {
			if (isPE32)
				RawValue32.ImageBase = (uint)value;
			else
				RawValue64.ImageBase = value;
		}
	}

	/// <summary/>
	public uint SectionAlignment {
		get => isPE32 ? RawValue32.SectionAlignment : RawValue64.SectionAlignment;
		set {
			if (isPE32)
				RawValue32.SectionAlignment = value;
			else
				RawValue64.SectionAlignment = value;
		}
	}

	/// <summary/>
	public uint FileAlignment {
		get => isPE32 ? RawValue32.FileAlignment : RawValue64.FileAlignment;
		set {
			if (isPE32)
				RawValue32.FileAlignment = value;
			else
				RawValue64.FileAlignment = value;
		}
	}

	/// <summary/>
	public ushort MajorOperatingSystemVersion {
		get => isPE32 ? RawValue32.MajorOperatingSystemVersion : RawValue64.MajorOperatingSystemVersion;
		set {
			if (isPE32)
				RawValue32.MajorOperatingSystemVersion = value;
			else
				RawValue64.MajorOperatingSystemVersion = value;
		}
	}

	/// <summary/>
	public ushort MinorOperatingSystemVersion {
		get => isPE32 ? RawValue32.MinorOperatingSystemVersion : RawValue64.MinorOperatingSystemVersion;
		set {
			if (isPE32)
				RawValue32.MinorOperatingSystemVersion = value;
			else
				RawValue64.MinorOperatingSystemVersion = value;
		}
	}

	/// <summary/>
	public ushort MajorImageVersion {
		get => isPE32 ? RawValue32.MajorImageVersion : RawValue64.MajorImageVersion;
		set {
			if (isPE32)
				RawValue32.MajorImageVersion = value;
			else
				RawValue64.MajorImageVersion = value;
		}
	}

	/// <summary/>
	public ushort MinorImageVersion {
		get => isPE32 ? RawValue32.MinorImageVersion : RawValue64.MinorImageVersion;
		set {
			if (isPE32)
				RawValue32.MinorImageVersion = value;
			else
				RawValue64.MinorImageVersion = value;
		}
	}

	/// <summary/>
	public ushort MajorSubsystemVersion {
		get => isPE32 ? RawValue32.MajorSubsystemVersion : RawValue64.MajorSubsystemVersion;
		set {
			if (isPE32)
				RawValue32.MajorSubsystemVersion = value;
			else
				RawValue64.MajorSubsystemVersion = value;
		}
	}

	/// <summary/>
	public ushort MinorSubsystemVersion {
		get => isPE32 ? RawValue32.MinorSubsystemVersion : RawValue64.MinorSubsystemVersion;
		set {
			if (isPE32)
				RawValue32.MinorSubsystemVersion = value;
			else
				RawValue64.MinorSubsystemVersion = value;
		}
	}

	/// <summary/>
	public uint Win32VersionValue {
		get => isPE32 ? RawValue32.Win32VersionValue : RawValue64.Win32VersionValue;
		set {
			if (isPE32)
				RawValue32.Win32VersionValue = value;
			else
				RawValue64.Win32VersionValue = value;
		}
	}

	/// <summary/>
	public uint SizeOfImage {
		get => isPE32 ? RawValue32.SizeOfImage : RawValue64.SizeOfImage;
		set {
			if (isPE32)
				RawValue32.SizeOfImage = value;
			else
				RawValue64.SizeOfImage = value;
		}
	}

	/// <summary/>
	public uint SizeOfHeaders {
		get => isPE32 ? RawValue32.SizeOfHeaders : RawValue64.SizeOfHeaders;
		set {
			if (isPE32)
				RawValue32.SizeOfHeaders = value;
			else
				RawValue64.SizeOfHeaders = value;
		}
	}

	/// <summary/>
	public uint CheckSum {
		get => isPE32 ? RawValue32.CheckSum : RawValue64.CheckSum;
		set {
			if (isPE32)
				RawValue32.CheckSum = value;
			else
				RawValue64.CheckSum = value;
		}
	}

	/// <summary/>
	public Subsystem Subsystem {
		get => (Subsystem)(isPE32 ? RawValue32.Subsystem : RawValue64.Subsystem);
		set {
			if (isPE32)
				RawValue32.Subsystem = (ushort)value;
			else
				RawValue64.Subsystem = (ushort)value;
		}
	}

	/// <summary/>
	public DllCharacteristics DllCharacteristics {
		get => (DllCharacteristics)(isPE32 ? RawValue32.DllCharacteristics : RawValue64.DllCharacteristics);
		set {
			if (isPE32)
				RawValue32.DllCharacteristics = (ushort)value;
			else
				RawValue64.DllCharacteristics = (ushort)value;
		}
	}

	/// <summary/>
	public ulong SizeOfStackReserve {
		get => isPE32 ? RawValue32.SizeOfStackReserve : RawValue64.SizeOfStackReserve;
		set {
			if (isPE32)
				RawValue32.SizeOfStackReserve = (uint)value;
			else
				RawValue64.SizeOfStackReserve = value;
		}
	}

	/// <summary/>
	public ulong SizeOfStackCommit {
		get => isPE32 ? RawValue32.SizeOfStackCommit : RawValue64.SizeOfStackCommit;
		set {
			if (isPE32)
				RawValue32.SizeOfStackCommit = (uint)value;
			else
				RawValue64.SizeOfStackCommit = value;
		}
	}

	/// <summary/>
	public ulong SizeOfHeapReserve {
		get => isPE32 ? RawValue32.SizeOfHeapReserve : RawValue64.SizeOfHeapReserve;
		set {
			if (isPE32)
				RawValue32.SizeOfHeapReserve = (uint)value;
			else
				RawValue64.SizeOfHeapReserve = value;
		}
	}

	/// <summary/>
	public ulong SizeOfHeapCommit {
		get => isPE32 ? RawValue32.SizeOfHeapCommit : RawValue64.SizeOfHeapCommit;
		set {
			if (isPE32)
				RawValue32.SizeOfHeapCommit = (uint)value;
			else
				RawValue64.SizeOfHeapCommit = value;
		}
	}

	/// <summary/>
	public uint LoaderFlags {
		get => isPE32 ? RawValue32.LoaderFlags : RawValue64.LoaderFlags;
		set {
			if (isPE32)
				RawValue32.LoaderFlags = value;
			else
				RawValue64.LoaderFlags = value;
		}
	}

	/// <summary/>
	public uint NumberOfRvaAndSizes {
		get => isPE32 ? RawValue32.NumberOfRvaAndSizes : RawValue64.NumberOfRvaAndSizes;
		set {
			if (isPE32)
				RawValue32.NumberOfRvaAndSizes = value;
			else
				RawValue64.NumberOfRvaAndSizes = value;
		}
	}

	/// <summary/>
	public PESpanList<DataDirectory> DataDirectories { get; }
	#endregion

	/// <summary>
	/// Magic == <see cref="IMAGE_NT_OPTIONAL_HDR64_MAGIC"/>
	/// </summary>
	public bool IsPE64 => !isPE32;

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_EXPORT
	/// </summary>
	public DataDirectory ExportDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_EXPORT);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_IMPORT
	/// </summary>
	public DataDirectory ImportDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_IMPORT);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_RESOURCE
	/// </summary>
	public DataDirectory ResourceDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_RESOURCE);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_EXCEPTION
	/// </summary>
	public DataDirectory ExceptionDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_EXCEPTION);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_SECURITY
	/// </summary>
	public DataDirectory SecurityDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_SECURITY);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_BASERELOC
	/// </summary>
	public DataDirectory BaseRelocationDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_BASERELOC);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_DEBUG
	/// </summary>
	public DataDirectory DebugDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_DEBUG);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_TLS
	/// </summary>
	public DataDirectory ThreadLocalStorageDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_TLS);

	/// <summary>
	/// IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG
	/// </summary>
	public DataDirectory LoadConfigDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG);

	/// <remarks>
	/// IMAGE_DIRECTORY_ENTRY_IAT
	/// </remarks>
	public DataDirectory ImportAddressDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_IAT);

	/// <remarks>
	/// IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT
	/// </remarks>
	public DataDirectory DelayImportDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT);

	/// <remarks>
	/// IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR
	/// </remarks>
	public DataDirectory ComDescriptorDirectory => LookupDataDirectory(IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR);

	internal OptionalHeader(PEImage peImage) {
		this.peImage = peImage;
		DataDirectories = new PESpanList<DataDirectory>(() => new DataDirectory(), () => (int)NumberOfRvaAndSizes, peImage.LoadOptions.MaximumNumberOfRvaAndSizes, peImage.LoadOptions.TruncateIfExceeded);
	}

	internal override void Load(ref LoadContext context) {
		var startData = context.Data;
		var oldData = RawData;
		LoadSelf(ref context);
		bool selfLoaded = context.Next;
		// Parse IMAGE_OPTIONAL_HEADER fields (without data directories)

		DataDirectories.Load(ref context);
		// Parse IMAGE_OPTIONAL_HEADER.DataDirectory

		if (selfLoaded) {
			int length = context.Next ? peImage.FileHeader.SizeOfOptionalHeader : context.Data.Index - startData.Index;
			SetLoaded(ref context.Data, startData, oldData, length, !context.Next);
		}
	}

	internal void LoadSelf(ref LoadContext context) {
		if (!PreLoad(ref context))
			return;
		if (context.Data.Length < 4) {
			SetError(ref context.Next);
			return;
		}
		var fileHeader = peImage.FileHeader;
		if (fileHeader.IsEmpty) {
			SetError(ref context.Next);
			return;
		}
		ushort magic = context.Data.Read<ushort>(0);
		if (magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
			isPE32 = true;
		else if (magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC)
			isPE32 = false;
		else if (peImage.LoadOptions.CheckSignature) {
			SetError(ref context.Next);
			return;
		}
		else
			isPE32 = !fileHeader.Machine.Is64Bit();
		ushort sizeOfOptionalHeader = fileHeader.SizeOfOptionalHeader;
		if (sizeOfOptionalHeader < (isPE32 ? 0x60u : 0x70) || sizeOfOptionalHeader > peImage.LoadOptions.MaximumSizeOfOptionalHeader || context.Data.Length < sizeOfOptionalHeader) {
			SetError(ref context.Next);
			return;
		}
		SetLoaded(ref context.Data, isPE32 ? 0x60 : 0x70, true);
	}

	static DataDirectory CreateNotExistDataDirectory() {
		var dataDirectory = new DataDirectory();
		var context = new LoadContext();
		dataDirectory.Load(ref context);
		return dataDirectory;
	}

	DataDirectory LookupDataDirectory(int index) {
		return index < DataDirectories.Count ? DataDirectories[index] : notExistDataDirectory;
	}
}
