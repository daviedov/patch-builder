using System.Collections;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;

namespace IxPatchBuilder
{
	public class Comparer
	{
		string[] patterns = new string[] { @"\[assembly\: AssemblyFileVersion\(""[\d\.]+""\)\]" };

		public void CompareFolders(string oldPath, string newPath, string pathcPath)
		{
			CompareFilesInFolder(oldPath, newPath, pathcPath);
			//string[] subdirectories1 = Directory.GetDirectories(folder1Path);
			string[] newSubdirectories = Directory.GetDirectories(newPath);

			foreach (string newSubdirectory in newSubdirectories)
			{
				string newDirectoryName = Path.GetFileName(newSubdirectory);
				string oldDirectory = Path.Combine(oldPath, newDirectoryName);

				if (Directory.Exists(oldDirectory))
				{
					CompareFolders(oldDirectory, newSubdirectory, Path.Combine(pathcPath, newDirectoryName));
				}
				else
				{
					Console.WriteLine($"Subdirectory {newDirectoryName} does not exist in folder 2.");
					if (!Directory.Exists(Path.Combine(pathcPath, newDirectoryName)))
					{
						Directory.CreateDirectory(Path.Combine(pathcPath, newDirectoryName));
					}
					foreach (var srcPath in Directory.GetFiles(newSubdirectory))
					{
						//Copy the file from sourcepath and place into mentioned target path, 
						//Overwrite the file if same file is exist in target path

						string dstPath = Path.Combine(pathcPath, newDirectoryName, Path.GetFileName(srcPath));
						File.Copy(srcPath, dstPath, true);
					}

					//File.Copy(subdirectory2, Path.Combine(folder3Path, subdirectoryName), true);
				}
			}

		}

		public void CompareFilesInFolder(string oldPath, string newPath, string folder3Path)
		{
			if (!Directory.Exists(folder3Path))
			{
				Directory.CreateDirectory(folder3Path);
			}

			string[] newFiles = Directory.GetFiles(newPath);

			Console.WriteLine("Comparing files in two folders...");

			foreach (string newFile in newFiles)
			{
				string fileName = Path.GetFileName(newFile);
				string oldFile = Path.Combine(oldPath, fileName);

				if (File.Exists(oldFile) && AreFilesEqual(newFile, oldFile))
				{
					continue;
				}
				Console.WriteLine($"{fileName} is different in both folders.");
				File.Copy(newFile, Path.Combine(folder3Path, fileName), true);
			}

		}

		public void DeleteEmptyFolders(string directoryPath)
		{
			try
			{
				// Get a list of all subdirectories in the specified directory
				string[] subdirectories = Directory.GetDirectories(directoryPath);

				foreach (string subdirectory in subdirectories)
				{
					// Recursively delete empty subdirectories
					DeleteEmptyFolders(subdirectory);

					// Check if the current subdirectory is empty
					if (Directory.GetFiles(subdirectory).Length == 0 && Directory.GetDirectories(subdirectory).Length == 0)
					{
						// If it's empty, delete the subdirectory
						Directory.Delete(subdirectory);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}

		private bool AreFilesEqual(string filePath1, string filePath2)
		{
			using var hash1 = SHA256.Create();
			using var hash2 = SHA256.Create();
			using var stream1 = File.OpenRead(filePath1);
			using var stream2 = File.OpenRead(filePath2);
			byte[] hashBytes1 = hash1.ComputeHash(stream1);
			byte[] hashBytes2 = hash2.ComputeHash(stream2);
			if (StructuralComparisons.StructuralEqualityComparer.Equals(hashBytes1, hashBytes2))
			{
				return true;
			}
			return AreFilesSourceEqual(filePath1, filePath2);
		}

		private bool AreFilesSourceEqual(string filePath1, string filePath2)
		{
			try
			{
				var decompiler1 = new CSharpDecompiler(filePath1, new DecompilerSettings());
				var decompiler2 = new CSharpDecompiler(filePath2, new DecompilerSettings());
				string[] code1 = decompiler1.DecompileWholeModuleAsString().Split("\r\n");
				string[] code2 = decompiler2.DecompileWholeModuleAsString().Split("\r\n");
				string[] mismatches = code2.Except(code1).ToArray();
				mismatches = ExceptionRules(mismatches);

				if (mismatches.Length > 0)
				{
					Console.WriteLine(filePath2);
					foreach (var m in mismatches)
					{
						Console.WriteLine(m);
					}
					Console.WriteLine();
				}

				return mismatches.Length == 0;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		private string[] ExceptionRules(string[] mismatches)
		{
			List<string> realMismatches = new List<string>();
			foreach (string mismatch in mismatches)
			{
				bool ignore = false;
				foreach (string pattern in patterns)
				{
					Match m = Regex.Match(mismatch, pattern, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						ignore = true;
						Console.WriteLine("Ignore mismatch '{0}.", mismatch);
						break;
					}
				}

				if (ignore)
				{
					continue;
				}

				realMismatches.Add(mismatch);
			}
			return realMismatches.ToArray();
		}
	}
}
