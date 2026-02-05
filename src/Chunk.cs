using RegionFileLib;
using SegmentLib;

namespace ChunkLib;

class Chunk
{
	public bool IS_EMPTY = false;
	public bool IS_CORRUPTED = false;

	Segment homeSegment;
	Pos chunkPos;
	Region chunkRegion;
		
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

		int x = chunkNum % 31;
		int y = chunkNum / 32; //Integer division is implicit

		Console.WriteLine($"{x} {y}");

	}
}