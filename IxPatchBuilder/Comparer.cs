using System.Collections;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.Extensions.Logging;

namespace IxPatchBuilder
{
	public class Comparer
	{
		private const string _separator = "\r\n";

		private readonly Rules _rules;
		private readonly ILogger _logger;

		public Comparer(Rules rules, ILogger logger)
		{
			_rules = rules;
			_logger = logger;
		}

		public void CompareFolders(string pathOld, string pathNew, string pathPatch)
		{
			//Compare files
			CompareFilesInFolder(pathOld, pathNew, pathPatch);

			//Compare subfolders
			string[] newSubdirectories = Directory.GetDirectories(pathNew);

			foreach (string newDirectory in newSubdirectories)
			{
				string newDirectoryName = Path.GetFileName(newDirectory);
				if (IgnoreFolder((newDirectoryName)))
				{
					continue;
				}

				string oldDirectory = Path.Combine(pathOld, newDirectoryName);

				if (Directory.Exists(oldDirectory))
				{
					CompareFolders(oldDirectory, newDirectory, Path.Combine(pathPatch, newDirectoryName));
				}
				else
				{
					_logger.LogInformation($"Directory {newDirectoryName} does not exist in old.");
					if (!Directory.Exists(Path.Combine(pathPatch, newDirectoryName)))
					{
						Directory.CreateDirectory(Path.Combine(pathPatch, newDirectoryName));
					}
					foreach (var srcPath in Directory.GetFiles(newDirectory))
					{
						//Copy the file from new path and place into patch path, 
						string dstPath = Path.Combine(pathPatch, newDirectoryName, Path.GetFileName(srcPath));
						File.Copy(srcPath, dstPath, true);
					}
				}
			}

		}

		public void CompareFilesInFolder(string pathOld, string pathNew, string pathPatch)
		{
			if (!Directory.Exists(pathPatch))
			{
				Directory.CreateDirectory(pathPatch);
			}

			string[] newFiles = Directory.GetFiles(pathNew);

			foreach (string newFile in newFiles)
			{
				string fileName = Path.GetFileName(newFile);
				if (IgnoreFile(fileName))
				{
					_logger.LogInformation($"Ignore file {fileName}.");
					continue;
				}

				string oldFile = Path.Combine(pathOld, fileName);

				if (File.Exists(oldFile) && AreFilesEqual(newFile, oldFile))
				{
					continue;
				}
				_logger.LogInformation($"{fileName} is different in both folders.");
				File.Copy(newFile, Path.Combine(pathPatch, fileName), true);
			}

		}

		public void DeleteEmptyFolders(string directoryPath)
		{
			try
			{
				// Get a list of all subdirectories in the specified directory
				string[] subDirectories = Directory.GetDirectories(directoryPath);

				foreach (string directory in subDirectories)
				{
					// Recursively delete empty subdirectories
					DeleteEmptyFolders(directory);

					// Check if the current directory is empty
					if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
					{
						// If it's empty, delete the directory
						Directory.Delete(directory);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"An error occurred: {ex.Message}");
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
			string fileName = Path.GetFileName(filePath1);
			if (IgnoreDecompileFile(fileName))
			{
				_logger.LogInformation($"Ignore decompile file {fileName}.");
				return false;
			}

			try
			{
				var decompiler1 = new CSharpDecompiler(filePath1, new DecompilerSettings());
				var decompiler2 = new CSharpDecompiler(filePath2, new DecompilerSettings());
				string[] code1 = decompiler1.DecompileWholeModuleAsString().Split(_separator);
				string[] code2 = decompiler2.DecompileWholeModuleAsString().Split(_separator);
				string[] mismatches = code2.Except(code1).ToArray();
				mismatches = DecompileExceptionRules(mismatches, fileName);

				if (mismatches.Length > 0)
				{
					_logger.LogTrace(filePath2);
					foreach (var m in mismatches)
					{
						_logger.LogTrace(m);
					}
					_logger.LogTrace(String.Empty);
				}

				return mismatches.Length == 0;
			}
			catch (Exception ex)
			{
				_logger.LogError($"An error occurred when decompile {fileName}: {ex.Message}");
				return false;
			}
		}

		private string[] DecompileExceptionRules(string[] mismatches, string fileName)
		{
			List<string> realMismatches = new List<string>();
			foreach (string mismatch in mismatches)
			{
				bool ignore = false;
				foreach (string pattern in _rules.Decompile.IgnorePatterns)
				{
					Match m = Regex.Match(mismatch, pattern, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						ignore = true;
						_logger.LogTrace("Ignore mismatch '{0} in file {1}.", mismatch, fileName);
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

		private bool IgnoreFile(string fileName)
		{
			return _rules.IgnoreFilePatterns.Select(pattern => Regex.Match(fileName, pattern, RegexOptions.IgnoreCase)).Any(m => m.Success);
		}

		private bool IgnoreFolder(string folderName)
		{
			return _rules.IgnoreFolderPatterns.Select(pattern => Regex.Match(folderName, pattern, RegexOptions.IgnoreCase)).Any(m => m.Success);
		}

		private bool IgnoreDecompileFile(string fileName)
		{
			return !_rules.Decompile.FilePatterns.Select(pattern => Regex.Match(fileName, pattern, RegexOptions.IgnoreCase)).Any(m => m.Success);
		}
	}
}
