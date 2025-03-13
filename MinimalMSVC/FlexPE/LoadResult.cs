namespace FlexPE;

/// <summary>
/// Represents the changes from the previous one
/// </summary>
public enum LoadDiff {
	/// <summary>
	/// No changes
	/// </summary>
	None,

	/// <summary>
	/// Previous <see cref="PESpan.IsEmpty"/> is <see langword="true"/> and now is non empty
	/// </summary>
	Added,

	/// <summary>
	/// Previous <see cref="PESpan.IsEmpty"/> is <see langword="false"/> and now is empty
	/// </summary>
	Deleted,

	/// <summary>
	/// <see cref="PESpan.IsEmpty"/> is still <see langword="true"/> but <see cref="PESpan.StartOffset"/> or <see cref="PESpan.EndOffset"/> is changed
	/// </summary>
	Moved
}

/// <summary>
/// Represents the state of current instance
/// </summary>
public enum LoadState {
	/// <summary>
	/// Error occurred when loading (only this state then <see cref="LoadContext.Next"/> will be set to <see langword="false"/>)
	/// </summary>
	Error = -1,

	/// <summary>
	/// Never loaded yet (just constructor is called)
	/// </summary>
	NotLoaded,

	/// <summary>
	/// Current component is not exist
	/// </summary>
	/// <remarks>
	/// Examples:
	/// <para />
	/// 1. <see cref="PEImage"/> may have no <see cref="ExportDirectory"/> so it is <see cref="NotExist"/>
	/// <para />
	/// 2. Error occurred when loading <see cref="FileHeader"/>, then all components after it like <see cref="OptionalHeader"/> will be considered to be <see cref="NotExist"/>
	/// </remarks>
	NotExist,

	/// <summary>
	/// Partly loaded (component itself is loaded but any subcomponent is <see cref="Error"/>)
	/// </summary>
	/// <remarks>
	/// Examples:
	/// <para />
	/// 1. <see cref="OptionalHeader"/> itself is loaded but its <see cref="OptionalHeader.DataDirectories"/> is <see cref="Error"/>, then <see cref="OptionalHeader"/> is <see cref="PartlyLoaded"/>
	/// <para />
	/// 2. Not all elements in <see cref="PESpanList{T}"/> are loaded then this <see cref="PESpanList{T}"/> is <see cref="PartlyLoaded"/>
	/// </remarks>
	PartlyLoaded,

	/// <summary>
	/// Fully loaded (component itself is loaded and all subcomponent are <see cref="NotExist"/> or <see cref="PartlyLoaded"/> or <see cref="Loaded"/>)
	/// </summary>
	/// <remarks>
	/// Examples:
	/// <para />
	/// 1. <see cref="OptionalHeader"/> itself is loaded but its <see cref="OptionalHeader.DataDirectories"/> is <see cref="PartlyLoaded"/>, then <see cref="OptionalHeader"/> is still considered to be <see cref="Loaded"/>
	/// </remarks>
	Loaded
}

/// <summary>
/// Represents the result of loading
/// </summary>
public readonly record struct LoadResult {
	/// <summary>
	/// Loading state
	/// </summary>
	public LoadState State { get; }

	/// <summary>
	/// Loading diff
	/// </summary>
	public LoadDiff Diff { get; }

	internal LoadResult(LoadState state, LoadDiff diff) {
		State = state;
		Diff = diff;
	}
}
