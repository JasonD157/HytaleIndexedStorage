using BlockHelper;
using static Globals.Globals;

namespace Corruption;

enum ChunkSide
{
	TOP,
	BOTTOM,
	LEFT,
	RIGHT
}



class SideMap
{
	private Dictionary<Guid, Dictionary<ChunkSide, Block[]>> sideMap;
    public SideMap()
	{
		sideMap = new();
	}

	public void MatchSide(Block[] matchArray)
	{
		var map = new Dictionary<ChunkSide, Block[]>();
		InitMap(map);
		ConvertArrayToSides(map, matchArray);


	}

    public void AddSidesFromTopLayer(Guid guid, Block[] blocks) //Only input blockarray which has i mapped to its proper world position
    {
		sideMap.Add(guid, new Dictionary<ChunkSide, Block[]>());
		var map = sideMap[guid];

		InitMap(map);
		ConvertArrayToSides(map, blocks);
    }

	private void InitMap(Dictionary<ChunkSide, Block[]> map)
	{
		foreach (ChunkSide side in Enum.GetValues<ChunkSide>())
		{
			map[side] = new Block[CHUNK_SIZE];
		}
	}

	private void ConvertArrayToSides(Dictionary<ChunkSide, Block[]> map, Block[] array)
	{
		 // Top
        for (uint i = 0; i < CHUNK_SIZE; i++)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.TOP][x] = array[i];

			Console.WriteLine($"TOP: {x}, {z}");
		}

        // Bottom
        for (uint i = CHUNK_AREA - CHUNK_SIZE; i < CHUNK_AREA; i++)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.BOTTOM][x] = array[i];

			Console.WriteLine($"BOTTOM: {x}, {z}");
		}

        // Left
        for (uint i = 0; i < CHUNK_AREA; i += CHUNK_SIZE)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.LEFT][z] = array[i];
			
			Console.WriteLine($"LEFT: {x}, {z}");
		}

		// Right
        for (uint i = 31; i < CHUNK_AREA; i += CHUNK_SIZE)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.RIGHT][z] = array[i];
			
			Console.WriteLine($"RIGHT: {x}, {z}");
		}
	}
}

static class SideMatcher
{
	public static int Match(Block[] side1, Block[] side2)
	{
		return 0;
	}
}