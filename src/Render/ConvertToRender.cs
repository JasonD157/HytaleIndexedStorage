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

	public static void Convert(BlockChunk chunk)
	{
		string json = File.ReadAllText("../../../src/Render/color_map.json");
		Dictionary<string, string> colormap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
		string defaultColor = "#FF0000"; //Bright Red

		Dictionary<string, float[]> voxels = new();

		for (uint i = 0; i < CHUNK_AREA * CHUNK_HEIGHT; i++)
		{
			Block block = chunk.GetBlock(i);
			if (block.blockName == Air_BlockName) continue; //Skip air, we don't render it.

			float[] color = Convert(colormap.GetValueOrDefault(block.blockName, defaultColor));
			if (colormap.GetValueOrDefault(block.blockName, defaultColor) == defaultColor) Console.WriteLine($"{block.blockName}");

			voxels.Add(Convert(block.pos), color);
		}

		string voxelJson = JsonConvert.SerializeObject(voxels);
		File.WriteAllText("../../../src/Render/Voxels/voxels.json", voxelJson);
	}
}