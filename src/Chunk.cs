using SegmentLib;
using Globals;
using static Globals.Globals;
using Newtonsoft.Json.Linq;
using BlockData;
using Corruption;
using BlockHelper;

namespace ChunkLib;

class Chunk
{
	public bool IS_EMPTY = false;
	public bool IS_CORRUPTED = false;
	

	public Segment homeSegment;
	public Pos_2D? chunkPos;
	public Region? chunkRegion;

	public BlockChunk blockChunk;
	public Block[] blocks;

	public Chunk(Segment homeSegment)
	{
		this.homeSegment = homeSegment;

		if (!homeSegment.IS_EMPTY)
		{

		}
		else
		{
			this.IS_EMPTY = true;
		}

		if (homeSegment.IS_CORRUPTED) this.IS_CORRUPTED = true;
	}
	
	public void GetBlockData()
	{
		if (IS_EMPTY) return;

		JObject json = homeSegment.GetBSON();
		JToken chunkColumn = json.SelectToken("Components.ChunkColumn.Sections");

		BlockChunk blockChunk = new BlockChunk();
		int sectionNumber = 0;
		foreach (JToken section in chunkColumn.Children())
		{
			int version = (int)section.SelectToken("Components.Block.Version");
			byte[] blockData = (byte[])section.SelectToken("Components.Block.Data");

			var reader = new BlockDataReader(blockData, blockChunk);
			sectionNumber += 1;
		}

		Pos_3D? mut = null;
		if (chunkPos is not null) mut = new Pos_3D(chunkPos.x, 0, chunkPos.z);

		this.blockChunk = blockChunk;
		this.blocks = blockChunk.GetBlocks(mut);
	}

	public void CalculateWorldPos(Region homeRegion, int chunkNum)
	{
		Pos_2D basePos = homeRegion.firstPos;

		int x = chunkNum & 31; //Column
		int z = chunkNum / 32; //Row

		this.chunkPos = basePos + new Pos_2D(x * CHUNK_SIZE, z * CHUNK_SIZE);
		this.chunkRegion = new Region(this.chunkPos, this.chunkPos + new Pos_2D(CHUNK_SIZE - 1, CHUNK_SIZE - 1));
	}
}