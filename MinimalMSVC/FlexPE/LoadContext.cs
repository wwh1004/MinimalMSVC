namespace FlexPE;

record struct LoadContext {
	public Segment Data;
	public bool Next;
	public LoadState? SetStateOnce;
}
