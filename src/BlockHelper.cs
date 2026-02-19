using Globals;
using static Globals.Globals;

namespace BlockHelper;

struct Block
{
	public Pos_3D pos;
	public string blockName;

	public Block(Pos_3D pos, string name)
	{
		this.pos = pos;
		blockName = name;
	}

	public Block(uint idx, string name)
	{
		pos = BlockHelper.GetChunkPos(idx);
		blockName = name;
	}

	public override string ToString()
	{
		return $"[{blockName} : {pos}]";
	}
}

class BlockChunk
{
	private uint[] blockKeys;
	private uint currIdx;
	private BlockMap map;

	public BlockChunk()
	{
		blockKeys = new uint[32 * 32 * 320];
		currIdx = 0;
		map = BlockMap.GetBlockMap();
	}

	public void AddBlock(string block)
	{
		blockKeys[currIdx] = map.AddBlock(block);
		currIdx++;
	}

	public uint GetKey(uint idx)
	{
		return blockKeys[idx];
	}

	public string GetBlock(uint idx)
	{
		return map.GetBlock(blockKeys[idx]);
	}

	public uint[] GetBlocks()
	{
		//Pls no mutate
		return blockKeys;
	}

	public uint[] GetKeySlice(uint x, uint z)
	{
		uint[] slice = new uint[CHUNK_HEIGHT];

		for (uint y = 0; y < CHUNK_HEIGHT; y++)
		{
			uint index = BlockHelper.GetChunkIndex(x, y, z);
			slice[y] = GetKey(index);
		}

		return slice;
	}

	public Block GetHeighestNonEmptyBlock(uint x, uint z)
	{
		for (uint y = CHUNK_HEIGHT - 1; y > 0; y--)
		{
			uint index = BlockHelper.GetChunkIndex(x, y, z);
			string blockName = GetBlock(index);
			if (blockName != Air_BlockName) return new Block(index, blockName);
		}

		throw new Exception("Whole slice is empty.");
	}

	public Block[] GetHeighestNonEmptyBlocks()
	{
		Block[] array = new Block[CHUNK_AREA];

		for (uint i = 0; i < CHUNK_AREA; i++)
		{
			uint x = i & 31;
			uint z = i / 32;
			array[i] = GetHeighestNonEmptyBlock(x, z);
		}

		return array;
	}
}

class BlockMap
{
	private uint blockCount = 1; //Start at 1 so default arrays (empty) throw an error when indexed into.
	private Dictionary<uint, string> blockMap = new();
	private Dictionary<string, uint> reverseBlockMap = new();

	//Singleton
	private BlockMap()
	{}
	private static readonly BlockMap _blockMap = new BlockMap();
	public static BlockMap GetBlockMap()
	{
		return _blockMap;
	}

	public uint AddBlock(string blockName)
	{
		if (reverseBlockMap.ContainsKey(blockName))
			return GetKey(blockName);

		blockMap[blockCount] = blockName;
		reverseBlockMap[blockName] = blockCount;

		return blockCount++; //Return blockcount, then increment
	}

	public string GetBlock(uint key)
	{
		string blockName = blockMap.GetValueOrDefault(key, "__Not Found__");
		return (blockName == "__Not Found__") ? "Empty" : blockName; //throw new Exception($"Tried to index non-existant key {key}") : block;
	}

	public uint GetKey(string blockName)
	{
		uint key = reverseBlockMap.GetValueOrDefault(blockName, uint.MaxValue);
		return (key == uint.MaxValue) ? 0 : key; //throw new Exception($"Tried to index non-existant block {block}") : key; //The error cases are "non-assigned" sections by Hytale. This happens when a whole section is empty (air).
	}
}

class BlockHelper
{
	public static Pos_3D GetChunkPos(uint columnIndex)
	{
		//For sections:
		//returns a value between (0,0,0) -> (31,31,31) formatted internally as [x,y,z]

		//Also works with chunks, but will obviously reach outside the values above.
		return new Pos_3D((int)columnIndex & 31, (int)columnIndex / 1024, ((int)columnIndex % 1024) / 32);
	}

	public static uint GetChunkIndex(uint x, uint y, uint z)
	{
		//For sections:
		//returns a value between 0 -> 1023 which represents an index into a 32x32x32 grid

		//Also works with chunks, but will obviously reach outside the values above.
		return x + y * 1024 + z * 32;
	}
}