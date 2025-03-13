using System;
using System.Runtime.CompilerServices;

namespace FlexPE;

/// <summary>
/// Image layout
/// </summary>
public enum ImageLayout {
	/// <summary>
	/// File layout (e.g. on disk)
	/// </summary>
	File,

	/// <summary>
	/// Memory layout which <see cref="FileOffset"/> equals <see cref="RVA"/> (e.g. loaded by OS loader)
	/// </summary>
	Memory
}

/// <summary>
/// Image format
/// </summary>
public enum ImageFormat {
	/// <summary>
	/// Portable Executable (e.g. a.exe, a.dll, a.sys)
	/// </summary>
	Executable,

	/// <summary>
	/// Common Object File Format (e.g. .obj)
	/// </summary>
	Object
}

/// <summary>
/// Portable Executable Image
/// </summary>
public sealed class PEImage : IRawData {
	Segment data;
	readonly bool isGrowable;
	readonly ExportDirectory exportDirectory;
	readonly ImportDirectory importDirectory;

	internal readonly LoadOptions LoadOptions;

	internal bool IsGrowable => isGrowable;

	/// <summary>
	/// Entire PE image data
	/// </summary>
	public Segment RawData {
		get => data;
		internal set => data = value;
	}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	/// <summary>
	/// Entire PE image data
	/// </summary>
	public Span<byte> RawSpan => data.Span;

	/// <summary>
	/// Entire PE image data
	/// </summary>
	public Memory<byte> RawMemory => data.Memory;
#endif

	/// <inheritdoc/>
	FileOffset IRawData.StartOffset => 0;

	/// <inheritdoc/>
	FileOffset IRawData.EndOffset => (FileOffset)data.Length;

	/// <summary>
	/// Current PE image layout
	/// </summary>
	public ImageLayout ImageLayout { get; }

	/// <summary>
	/// Current image format
	/// </summary>
	public ImageFormat ImageFormat { get; }

	/// <summary>
	/// Is current instance fully loaded without any error
	/// </summary>
	public bool IsLoadedNoError { get; private set; }

	/// <summary/>
	public DosHeader DosHeader { get; }

	/// <summary/>
	public NtHeaders NtHeaders { get; }

	/// <summary/>
	public FileHeader FileHeader => NtHeaders.FileHeader;

	/// <summary/>
	public OptionalHeader OptionalHeader => NtHeaders.OptionalHeader;

	/// <summary/>
	public PESpanList<SectionHeader> SectionHeaders { get; }

	/// <summary>
	/// ExportDirectory (lazy loading)
	/// </summary>
	public ExportDirectory ExportDirectory => EnsureLoaded(exportDirectory, OptionalHeader.ExportDirectory);

	/// <summary>
	/// ImportDirectory (lazy loading)
	/// </summary>
	public ImportDirectory ImportDirectory => EnsureLoaded(importDirectory, OptionalHeader.ImportDirectory);

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="data">Source</param>
	/// <param name="loadOptions">Loading options</param>
	/// <param name="imageLayout"><paramref name="data"/> is file layout or memory layout</param>
	/// <param name="imageFormat">Format of <paramref name="data"/>, set <see langword="null"/> to automatically detect or manually set one. <see cref="ImageFormat"/> represents the automatically detected format after constructor is called.</param>
	public PEImage(byte[] data, in LoadOptions loadOptions = default, ImageLayout imageLayout = ImageLayout.File, ImageFormat? imageFormat = null) : this(new Segment(data), loadOptions, imageLayout, imageFormat) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="data">Source</param>
	/// <param name="loadOptions">Loading options</param>
	/// <param name="imageLayout"><paramref name="data"/> is file layout or memory layout</param>
	/// <param name="imageFormat">Format of <paramref name="data"/>, set <see langword="null"/> to automatically detect or manually set one. <see cref="ImageFormat"/> represents the automatically detected format after constructor is called.</param>
	public unsafe PEImage(nuint data, in LoadOptions loadOptions = default, ImageLayout imageLayout = ImageLayout.File, ImageFormat? imageFormat = null) : this(new Segment((byte*)data, int.MaxValue), loadOptions, imageLayout, imageFormat) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="data">Source</param>
	/// <param name="loadOptions">Loading options</param>
	/// <param name="imageLayout"><paramref name="data"/> is file layout or memory layout</param>
	/// <param name="imageFormat">Format of <paramref name="data"/>, set <see langword="null"/> to automatically detect or manually set one. <see cref="ImageFormat"/> represents the automatically detected format after constructor is called.</param>
	public unsafe PEImage(byte* data, in LoadOptions loadOptions = default, ImageLayout imageLayout = ImageLayout.File, ImageFormat? imageFormat = null) : this(new Segment(data, int.MaxValue), loadOptions, imageLayout, imageFormat) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="data">Source</param>
	/// <param name="loadOptions">Loading options</param>
	/// <param name="imageLayout"><paramref name="data"/> is file layout or memory layout</param>
	/// <param name="imageFormat">Format of <paramref name="data"/>, set <see langword="null"/> to automatically detect or manually set one. <see cref="ImageFormat"/> represents the automatically detected format after constructor is called.</param>
	public PEImage(Segment data, in LoadOptions loadOptions = default, ImageLayout imageLayout = ImageLayout.File, ImageFormat? imageFormat = null) {
		this.data = data;
		//LoadOptions = loadOptions;
		ImageLayout = imageLayout;
		ImageFormat = GetImageFormat(data, imageLayout, imageFormat);
		LoadOptions = !loadOptions.HasValue ? (ImageFormat == ImageFormat.Executable ? LoadOptions.ExecutableDefault : LoadOptions.ObjectDefault) : loadOptions;
		isGrowable = !data.IsUnmanaged && ImageFormat == ImageFormat.Executable;
		DosHeader = new DosHeader(this);
		NtHeaders = new NtHeaders(this);
		SectionHeaders = new PESpanList<SectionHeader>(() => new SectionHeader(this), () => FileHeader.NumberOfSections, LoadOptions.MaximumNumberOfSections, LoadOptions.TruncateIfExceeded);
		exportDirectory = new ExportDirectory(this);
		importDirectory = new ImportDirectory(this);
		var context = new LoadContext {
			Data = data,
			Next = true
		};
		Load(ref context);
		IsLoadedNoError = context.Next;
		if (data.IsUnmanaged) {
			int imageSize = context.Next ? GetImageSize() : data.Index;
			this.data = this.data[..imageSize];
		}
	}

	/// <summary>
	/// Reloads current instance
	/// </summary>
	/// <returns></returns>
	public bool Reload() {
		var context = new LoadContext {
			Data = data,
			Next = true
		};
		Load(ref context);
		return IsLoadedNoError = context.Next;
	}

	internal void Load(ref LoadContext context) {
		var data = context.Data;
		if (ImageFormat == ImageFormat.Executable) {
			DosHeader.Load(ref context);
			// Parse IMAGE_DOS_HEADER

			uint lfanew = DosHeader.Lfanew;
			if (lfanew > LoadOptions.MaximumLfanew || data.Length < (int)lfanew)
				context.SetStateOnce = LoadState.Error;
			else
				context.Data = data[(int)lfanew..];
			NtHeaders.Load(ref context);
			// Parse IMAGE_NT_HEADERS
		}
		else {
			context.Next = false;
			DosHeader.Load(ref context);
			NtHeaders.Load(ref context);
			context.Next = true;
			NtHeaders.FileHeader.Load(ref context);
			// Parse IMAGE_FILE_HEADER
		}

		SectionHeaders.Load(ref context);
		// Parse IMAGE_SECTION_HEADERs
	}

	static ImageFormat GetImageFormat(in Segment data, ImageLayout imageLayout, ImageFormat? imageFormat) {
		if (imageLayout == ImageLayout.Memory) {
			if (imageFormat == ImageFormat.Object)
				throw new ArgumentOutOfRangeException(nameof(imageFormat));
			return ImageFormat.Executable;
		}
		// Must be PE file
		if (imageFormat is ImageFormat f)
			return f;
		// Already set
		if (!data.TryRead(0, out ushort magic))
			return ImageFormat.Executable;
		if (magic == NativeMethods.IMAGE_DOS_SIGNATURE)
			return ImageFormat.Executable;
		switch ((Machine)magic) {
		case Machine.I386:
		case Machine.ARM:
		case Machine.IA64:
		case Machine.AMD64:
		case Machine.ARM64:
			return ImageFormat.Object;
		default:
			return ImageFormat.Executable;
		}
		// Guess one
	}

	int GetImageSize() {
		var sectionHeaders = SectionHeaders;
		if (ImageFormat == ImageFormat.Executable) {
			uint alignment = ImageLayout == ImageLayout.File ? OptionalHeader.FileAlignment : OptionalHeader.SectionAlignment;
			if (sectionHeaders.Count != 0) {
				if (ImageLayout == ImageLayout.Memory)
					return (int)Math.Min(AlignUp((ulong)sectionHeaders[^1].VirtualAddress + sectionHeaders[^1].VirtualSize, alignment), int.MaxValue);
				for (int i = sectionHeaders.Count - 1; i >= 0; i--) {
					if (sectionHeaders[i].SizeOfRawData != 0)
						return (int)Math.Min((ulong)sectionHeaders[i].PointerToRawData + sectionHeaders[i].SizeOfRawData, int.MaxValue);
				}
			}
			return (int)AlignUp(OptionalHeader.SizeOfHeaders, alignment);
		}
		else {
			return int.MaxValue;
			// TODO: Get image size for object file
		}
	}

	T EnsureLoaded<T>(T component, DataDirectory dataDirectory) where T : PESpan {
		if (component.LoadResult.State == LoadState.NotLoaded) {
			FileOffset offset;
			if (dataDirectory.LoadResult.State != LoadState.Loaded || dataDirectory.VirtualAddress == 0) {
				var context = new LoadContext();
				component.Load(ref context);
			}
			else if ((offset = ToFileOffset(dataDirectory.VirtualAddress)) == 0) {
				var context = new LoadContext {
					Next = true,
					SetStateOnce = LoadState.Error
				};
				component.Load(ref context);
			}
			else {
				var context = new LoadContext {
					Data = data[(int)offset..],
					Next = true
				};
				component.Load(ref context);
			}
		}
		return component;
	}

	/// <summary>
	/// Returns the first <see cref="SectionHeader"/> that has data at file offset (ignores <see cref="ImageLayout"/>)
	/// <paramref name="offset"/>
	/// </summary>
	/// <param name="offset">The file offset</param>
	/// <returns></returns>
	public SectionHeader? ToSectionHeader(FileOffset offset) {
		foreach (var section in SectionHeaders) {
			if (offset >= section.PointerToRawData && offset < section.PointerToRawData + section.SizeOfRawData)
				return section;
		}
		return null;
	}

	/// <summary>
	/// Returns the first <see cref="SectionHeader"/> that has data at RVA (ignores <see cref="ImageLayout"/>)
	/// <paramref name="rva"/>
	/// </summary>
	/// <param name="rva">The RVA</param>
	/// <returns></returns>
	public SectionHeader? ToSectionHeader(RVA rva) {
		uint alignment = OptionalHeader.SectionAlignment;
		foreach (var section in SectionHeaders) {
			if (rva >= section.VirtualAddress && rva < section.VirtualAddress + AlignUp(section.VirtualSize, alignment))
				return section;
		}
		return null;
	}

	/// <summary>
	/// Converts a <see cref="FileOffset"/> to an <see cref="RVA"/>, returns 0 if out of range
	/// </summary>
	/// <param name="offset">The file offset to convert</param>
	/// <returns>The RVA</returns>
	public RVA ToRVA(FileOffset offset) {
		if (ImageLayout == ImageLayout.Memory)
			return (RVA)offset;

		// In pe headers
		var sectionHeaders = SectionHeaders;
		if (sectionHeaders.Count == 0)
			return (RVA)offset;

		// In pe additional data, like digital signature, won't be loaded into memory
		for (int i = sectionHeaders.Count - 1; i >= 0; i--) {
			if (sectionHeaders[i].SizeOfRawData == 0)
				continue;
			if (offset > sectionHeaders[i].PointerToRawData + sectionHeaders[i].SizeOfRawData)
				return 0;
			break;
		}

		// In a section
		var section = ToSectionHeader(offset);
		if (section is not null)
			return offset - section.PointerToRawData + section.VirtualAddress;

		// In pe headers
		return (RVA)offset;
	}

	/// <summary>
	/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>, returns 0 if out of range
	/// </summary>
	/// <param name="rva">The RVA to convert</param>
	/// <returns>The file offset</returns>
	public FileOffset ToFileOffset(RVA rva) {
		if (ImageLayout == ImageLayout.Memory)
			return (FileOffset)rva;

		// Check if rva is larger than memory layout size
		if ((uint)rva >= OptionalHeader.SizeOfImage)
			return 0;

		var section = ToSectionHeader(rva);
		if (section is not null) {
			uint offset = rva - section.VirtualAddress;
			// Virtual size may be bigger than raw size and there may be no corresponding file offset to rva
			if (offset < section.SizeOfRawData)
				return offset + section.PointerToRawData;
			return 0;
		}

		// If not in any section, rva is in pe headers and don't convert it
		return (FileOffset)rva;
	}

	static uint AlignUp(uint value, uint alignment) {
		return (value + alignment - 1) & ~(alignment - 1);
	}

	static ulong AlignUp(ulong value, uint alignment) {
		return (value + alignment - 1) & ~(alignment - 1);
	}

	/// <summary>
	/// Reads an ASCII string from <paramref name="rva"/> with <see cref="LoadOptions"/>
	/// </summary>
	/// <param name="rva"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadAsciiString(RVA rva) {
		return CreateSegment(rva).ReadAsciiString(LoadOptions.MaximumStringLength);
	}

	/// <summary>
	/// Reads an ASCII string from <paramref name="data"/> with <see cref="LoadOptions"/>
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadAsciiString(in Segment data) {
		return data.ReadAsciiString(LoadOptions.MaximumStringLength);
	}

	internal Segment CreateAsciiStringData(RVA rva) {
		var data = CreateSegment(rva);
		if (data.IsEmpty)
			return data;
		data = data[..data.GetAsciiStringLength(LoadOptions.MaximumStringLength)];
		return data;
	}

	/// <summary>
	/// Creates a segment from <paramref name="section"/>
	/// </summary>
	/// <param name="section"></param>
	/// <returns>Empty if out of range</returns>
	public Segment GetSectionData(SectionHeader? section) {
		if (section is null || section.IsEmpty)
			return default;
		if (section.SizeOfRawData == 0)
			return default;
		return CreateSegment(section.PointerToRawData, section.SizeOfRawData);
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="offset">Start offset</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(FileOffset offset) {
		return data.TrySlice((int)offset);
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="offset">Start offset</param>
	/// <param name="length">Length</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(FileOffset offset, int length) {
		return data.TrySlice((int)offset, length);
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="offset">Start offset</param>
	/// <param name="length">Length</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(FileOffset offset, uint length) {
		return CreateSegment(offset, (int)length);
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="rva">Start offset</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(RVA rva) {
		if (rva == 0)
			return data;
		var offset = ToFileOffset(rva);
		return offset != 0 ? data.TrySlice((int)offset) : default;
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="rva">Start offset</param>
	/// <param name="length">Length</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(RVA rva, int length) {
		if (rva == 0)
			return data.TrySlice(0, length);
		var offset = ToFileOffset(rva);
		return offset != 0 ? data.TrySlice((int)offset, length) : default;
	}

	/// <summary>
	/// Creates a segment from <see cref="RawData"/>
	/// </summary>
	/// <param name="rva">Start offset</param>
	/// <param name="length">Length</param>
	/// <returns>Empty if out of range</returns>
	public Segment CreateSegment(RVA rva, uint length) {
		return CreateSegment(rva, (int)length);
	}
}
