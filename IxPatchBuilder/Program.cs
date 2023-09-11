using IxPatchBuilder;

if (args.Length != 3)
{
	Console.WriteLine("Usage:");
	Console.WriteLine("IxPatchBuilder <path_to_old_folder> <path_to_new_folder> <path_to_patch_folder>");
	return;
}

Console.WriteLine($"Start time: {DateTime.Now}");
Comparer comparer = new Comparer();

comparer.CompareFolders(args[0], args[1], args[2]);
comparer.DeleteEmptyFolders(args[2]);
Console.WriteLine($"End time: {DateTime.Now}");
