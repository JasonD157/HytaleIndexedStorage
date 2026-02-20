using BlockHelper;
using static Globals.Globals;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Globals;

namespace ConvertToRender;

static class ConvertToRender
{
	public static float[] Convert(string hexColor)
	{
		string hex = hexColor.TrimStart('#');

		int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
		int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
		int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

		float rf = r / 255f;
		float gf = g / 255f;
		float bf = b / 255f;

		return [rf, gf, bf];
	}

	public static string Convert(Pos_3D pos)
	{
		return $"{pos.x},{pos.y},{pos.z}";
	}

	private static void GenerateVoxels(Block[] blocks, string id="")
	{
		string json = File.ReadAllText("../../../src/Render/color_map.json");
		Dictionary<string, string> colormap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
		string defaultColor = "#FF0000"; //Bright Red

		Dictionary<string, float[]> voxels = new();
		foreach (Block block in blocks)
		{
			if (block.blockName == Air_BlockName) continue; //Skip air, we don't render it.

			float[] color = Convert(colormap.GetValueOrDefault(block.blockName, defaultColor));
			if (colormap.GetValueOrDefault(block.blockName, defaultColor) == defaultColor) Console.WriteLine($"[WARN] Couldn't find a color for {block.blockName}");

			voxels.Add(Convert(block.pos), color);
		}

		string voxelJson = JsonConvert.SerializeObject(voxels);
		File.WriteAllText($"../../../src/Render/Voxels/{id}{((id == "") ? "temp" : "")}.voxels.json", voxelJson);
	}

	public static void Convert(BlockChunk chunk, Pos_3D? mut=null)
	{
		List<Block> blocks = new();
		for (uint i = 0; i < CHUNK_AREA * CHUNK_HEIGHT; i++)
		{
			blocks.Add(chunk.GetBlock(i, mut));
		}

		GenerateVoxels(blocks.ToArray());
	}

	public static void Convert(Block[] blocks)
	{
		GenerateVoxels(blocks);
	}
}