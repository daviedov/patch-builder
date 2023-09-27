namespace IxPatchBuilder;

public class Rules
{
	public string[] IgnoreFilePatterns { get; set; } = Array.Empty<string>();
	public string[] IgnoreFolderPatterns { get; set; } = Array.Empty<string>();
	public Decompile Decompile { get; set; } = new();
}