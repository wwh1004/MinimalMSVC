namespace FlexPE;

/// <summary>
/// File Offset
/// </summary>
public enum FileOffset : uint {
}

/// <summary>
/// Relative Virtual Address
/// </summary>
public enum RVA : uint {
}

partial class Extensions {
	/// <summary>
	/// Align up
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="alignment"></param>
	public static FileOffset AlignUp(this FileOffset offset, uint alignment) {
		return (FileOffset)(((uint)offset + alignment - 1) & ~(alignment - 1));
	}

	/// <summary>
	/// Align up
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="alignment"></param>
	public static FileOffset AlignUp(this FileOffset offset, int alignment) {
		return (FileOffset)(((uint)offset + alignment - 1) & ~(alignment - 1));
	}

	/// <summary>
	/// Align up
	/// </summary>
	/// <param name="rva"></param>
	/// <param name="alignment"></param>
	public static RVA AlignUp(this RVA rva, uint alignment) {
		return (RVA)(((uint)rva + alignment - 1) & ~(alignment - 1));
	}

	/// <summary>
	/// Align up
	/// </summary>
	/// <param name="rva"></param>
	/// <param name="alignment"></param>
	public static RVA AlignUp(this RVA rva, int alignment) {
		return (RVA)(((uint)rva + alignment - 1) & ~(alignment - 1));
	}
}
