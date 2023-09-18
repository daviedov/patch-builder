namespace IxPatchBuilder;

public class Rules
{
	public string[] IgnoreFilePatterns { get; set; }
	public string[] IgnoreFolderPatterns { get; set; }
	public Decompile Decompile { get; set; }
}