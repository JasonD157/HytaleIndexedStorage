using SegmentLib;
using Globals;
using static Globals.Globals;

namespace ChunkLib;

class Chunk
{
	public bool IS_EMPTY = false;
	public bool IS_CORRUPTED = false;
	

	public Segment homeSegment;
	public Pos chunkPos;
	public Region chunkRegion;
		
	public Chunk(Segment homeSegment, Region homeRegion, int chunkNum)
	{
		this.homeSegment = homeSegment;

		CalculateWorldPos(homeRegion, chunkNum);

		if (!homeSegment.IS_EMPTY)
		{
			
		}
		else
		{
			this.IS_EMPTY = true;
		}

		if (homeSegment.IS_CORRUPTED) this.IS_CORRUPTED = true;
	}

	public void CalculateWorldPos(Region homeRegion, int chunkNum)
	{
		Pos basePos = homeRegion.firstPos;

		int x = chunkNum % 31; //Column
		int z = chunkNum & 31; //Row

		this.chunkPos = basePos + new Pos(x * CHUNK_SIZE, z * CHUNK_SIZE);
		this.chunkRegion = new Region(this.chunkPos, this.chunkPos + new Pos(CHUNK_SIZE, CHUNK_SIZE));
	}
}