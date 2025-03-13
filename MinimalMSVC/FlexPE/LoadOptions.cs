namespace FlexPE;

/// <summary>
/// Options for loading PE image
/// </summary>
public record struct LoadOptions {
	internal bool HasValue { get; set; }

	/// <summary>
	/// Whether check structure magic/signature
	/// </summary>
	public bool CheckSignature { get; set; }

	/// <summary>
	/// If the size exceeds, truncate it and do not report an error. Otherwise, it is regarded as an error, set to <see cref="LoadState.Error"/> or return empty
	/// </summary>
	public bool TruncateIfExceeded { get; set; }

	/// <summary>
	/// Maximum length of string (e.g. IMAGE_SECTION_HEADER.Name)
	/// </summary>
	public int MaximumStringLength { get; set; }

	/// <summary>
	/// Maximum value of IMAGE_DOS_HEADER.e_lfanew
	/// </summary>
	public int MaximumLfanew { get; set; }

	/// <summary>
	/// Maximum value of IMAGE_FILE_HEADER.NumberOfSections
	/// </summary>
	public int MaximumNumberOfSections { get; set; }

	/// <summary>
	/// Maximum value of IMAGE_FILE_HEADER.SizeOfOptionalHeader
	/// </summary>
	public int MaximumSizeOfOptionalHeader { get; set; }

	/// <summary>
	/// Maximum value of IMAGE_FILE_HEADER.NumberOfRvaAndSizes
	/// </summary>
	public int MaximumNumberOfRvaAndSizes { get; set; }

	/// <summary>
	/// Maximum value of IMAGE_EXPORT_DIRECTORY.NumberOfFunctions
	/// </summary>
	public int MaximumNumberOfFunctions { get; set; }

	/// <summary>
	/// Maximum count of IMAGE_IMPORT_DESCRIPTOR in import directory
	/// </summary>
	public int MaximumImportModuleCount { get; set; }

	/// <summary>
	/// Maximum count of import function per module
	/// </summary>
	public int MaximumImportFunctionCount { get; set; }

	/// <summary>
	/// Default loading options for <see cref="ImageFormat.Executable"/>
	/// </summary>
	public static LoadOptions ExecutableDefault = new() {
		CheckSignature = true,
		TruncateIfExceeded = false,
		MaximumStringLength = 0x400,
		MaximumLfanew = 0x200,
		MaximumNumberOfSections = 0x100,
		MaximumSizeOfOptionalHeader = 0x200,
		MaximumNumberOfRvaAndSizes = 0x20,
		MaximumNumberOfFunctions = 0x4000,
		MaximumImportModuleCount = 0x100,
		MaximumImportFunctionCount = 0x2000
	};

	/// <summary>
	/// Default loading options for <see cref="ImageFormat.Object"/>
	/// </summary>
	public static LoadOptions ObjectDefault = new() {
		CheckSignature = true,
		TruncateIfExceeded = false,
		MaximumStringLength = 0x1000,
		MaximumLfanew = 0,
		MaximumNumberOfSections = ushort.MaxValue,
		MaximumSizeOfOptionalHeader = 0,
		MaximumNumberOfRvaAndSizes = 0,
		MaximumNumberOfFunctions = 0,
		MaximumImportModuleCount = 0,
		MaximumImportFunctionCount = 0
	};

	/// <summary>
	/// Constructor
	/// </summary>
	public LoadOptions() {
		HasValue = true;
	}
}
