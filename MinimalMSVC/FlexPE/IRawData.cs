namespace FlexPE;

/// <summary>
/// Represents the raw data in <see cref="PEImage"/>
/// </summary>
public interface IRawData {
	/// <summary>
	/// Raw data
	/// </summary>
	Segment RawData { get; }

	/// <summary>
	/// Start offset of current instance
	/// </summary>
	FileOffset StartOffset { get; }

	/// <summary>
	/// End offset of current instance
	/// </summary>
	FileOffset EndOffset { get; }
}
