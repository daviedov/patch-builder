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

		public void CompareFolders(string folder1Path, string folder2Path, string folder3Path)
		{
			string[] files1 = Directory.GetFiles(folder1Path);

			Console.WriteLine("Comparing files in two folders...");

			foreach (string file1 in files1)
			{
				string fileName = Path.GetFileName(file1);
				string file2 = Path.Combine(folder2Path, fileName);

				if (File.Exists(file2) && AreFilesEqual(file1, file2))
				{
					continue;
				}
				Console.WriteLine($"{fileName} is different in both folders.");
				File.Copy(file1, Path.Combine(folder3Path, fileName), true);
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
