// See https://aka.ms/new-console-template for more information

using IxPatchBuilder;

Console.WriteLine($"Start time: {DateTime.Now}");
string folder1Path = @"D:\10\4.3\Product"; // Replace with the path to the first folder
string folder2Path = @"D:\10\Main\Product"; // Replace with the path to the second folder
string folder3Path = @"D:\10\Patch\Product"; // Replace with the path to the second folder
Comparer comparer = new Comparer();

comparer.CompareFolders(folder1Path, folder2Path, folder3Path);
comparer.DeleteEmptyFolders(folder3Path);
Console.WriteLine($"End time: {DateTime.Now}");
