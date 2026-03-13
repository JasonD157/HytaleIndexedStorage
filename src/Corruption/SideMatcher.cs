using System.Numerics;
using System.Text.RegularExpressions;
using BlockHelper;
using Globals;
using static Globals.Globals;

namespace Corruption;

class SideMap
{
	const double MATCH_TRESHOLD = 99;

	private Dictionary<Guid, Dictionary<ChunkSide, Block[]>> sideMap;
	private Dictionary<Guid, Block[]> chunkLookup;
    public SideMap()
	{
		sideMap = new();
		chunkLookup = new();
	}

	public void MatchChunk(Block[] chunkToMatch)
	{
		var map = new Dictionary<ChunkSide, Block[]>();
		InitMap(map);
		ConvertArrayToSides(map, chunkToMatch);
		double highest = 0;
		foreach ((Guid guid, Dictionary<ChunkSide, Block[]> chunkSides) in sideMap)
		{
			var matchResults = SideMatcher.MatchChunk(map, chunkSides);

			foreach ((ChunkSide side, double confidence) in matchResults)
			{
				if (confidence > highest)
				{
					highest = confidence;
				}

				if (confidence >= MATCH_TRESHOLD / 100)
				{
					Console.WriteLine($"MATCH!!! Chunk {guid}, side {side} with a score of {confidence}");
					ConvertToRender.ConvertToRender.Convert(chunkToMatch, $"matchedChunkTo{guid}");
					ConvertToRender.ConvertToRender.Convert(chunkLookup[guid], $"{guid}");
					//throw new Exception();
				}
			}


		}
		Console.WriteLine($"Highest confidence score: {highest}");
	}

    public void AddSidesFromTopLayer(Guid guid, Block[] blocks) //Only input blockarray which has i mapped to its proper world position
	{
		chunkLookup.Add(guid, blocks);
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
		}

        // Bottom
        for (uint i = CHUNK_AREA - CHUNK_SIZE; i < CHUNK_AREA; i++)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.BOTTOM][x] = array[i];
		}

        // Left
        for (uint i = 0; i < CHUNK_AREA; i += CHUNK_SIZE)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.LEFT][z] = array[i];
		}

		// Right
        for (uint i = 31; i < CHUNK_AREA; i += CHUNK_SIZE)
		{
			uint x = i & 31;
			uint z = i / 32;

			map[ChunkSide.RIGHT][z] = array[i];
		}
	}
}

static class SideMatcher
{
	public static Dictionary<ChunkSide, double> MatchChunk(Dictionary<ChunkSide, Block[]> validChunk, Dictionary<ChunkSide, Block[]> invalidChunk)
	{
		//Confidence score of 100: perfect match of top blocks.

		//Keep in mind that only one side will match as two chunks only have one intersecting plane.

		Dictionary<ChunkSide, double> matchScores = new(); 

		foreach ((ChunkSide side, Block[] blocks) in validChunk)
		{
			Block[] correspondingSide = invalidChunk[sideInversion[side]];

			matchScores[side] = MatchBlocks(blocks, correspondingSide);
		}

		return matchScores;
	}

	private static double MatchBlocks(Block[] blocks, Block[] toMatch)
	{
		//Console.WriteLine("----------------------------Start Match");
		double totalConfidence = 0;

		for (int i = 0; i < CHUNK_SIZE; i++)
		{
			totalConfidence += MatchBlock(blocks[i], toMatch[i]);
		}

		double confidence = totalConfidence / CHUNK_SIZE; //Average the confidence scores
		//Console.WriteLine($"----------------------------End Match {confidence}");
		return confidence; 
	}

	//0-1 match based on Y value similarity.
	private static double MatchYValues(int first, int second)
	{
		const double SENSITIVITY = 0.075;

		double result = 1 / (
			SENSITIVITY *
			Math.Pow(first - second, 2)
			+ 1
		);

		return result;
	}

	private static double MatchBlock(Block first, Block second)
	{
		string type1 = first.blockName;
		string type2 = second.blockName;

		const double confidenceIfNoTypeMatch = 0.25;

		double result = (type1 == type2) ? 1 : confidenceIfNoTypeMatch;

		result *= MatchYValues(first.pos.y, second.pos.y);
		//Console.WriteLine($"Matching block {type1} to {type2}: {result}");
		return result;
	}
}