using static System.Diagnostics.Debug;
using System.IO;
using System.Reflection;
using System.Text;
using RegionFileLib;
using ChunkLib;

namespace Main;
class Program
{
	static void Main(string[]? args)
	{
		string path;
		if (args == null || args.Length == 0)
		{
			path = "../../../target";
		}
		else
		{
			path = args[0];
		}

		IterRegionFiles(path);
	}

	private static void IterRegionFiles(string target)
	{
		List<RegionFile> regionFiles = new List<RegionFile>();
		string[] regionFilePaths = Directory.GetFiles(target, "*", SearchOption.TopDirectoryOnly);
		foreach (string regionFilePath in regionFilePaths)
		{
			string fileName = Path.GetFileName(regionFilePath);
			string[] splits = fileName.Split(".");
			if (splits[^1] != "bin" || splits[^2] != "region")
			{
				Console.WriteLine($"[INFO] File {fileName} does not end with region.bin and thus will be skipped.");
				continue;
			}

			Console.WriteLine($"[INFO] Started processing region file {fileName}.");
			using (FileStream fs = File.OpenRead(regionFilePath))
			{
				BinaryReader reader = new BinaryReader(fs);

				RegionFileUtil regionFileUtil = new RegionFileUtil(reader, fileName);

				regionFiles.Add(regionFileUtil.HandleRegionFile());
			}
		}

		int empty = 0;
		int corrupted = 0;
		int filled = 0;
		foreach (RegionFile regionFile in regionFiles)
		{
			empty += regionFile.emptyChunks;
			corrupted += regionFile.corruptedChunks;
			filled += regionFile.filledChunks;
		}

		Console.WriteLine($"Empty chunks: {empty}");
		Console.WriteLine($"Corrupted chunks: {corrupted}");
		Console.WriteLine($"Filled (normal) chunks: {filled}");
	}
}