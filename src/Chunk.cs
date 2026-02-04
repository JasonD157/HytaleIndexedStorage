using RegionFileLib;
using SegmentLib;

namespace ChunkLib;

class Chunk
{
	protected BinaryReader r;

	public bool IS_EMPTY = false;
	public bool IS_CORRUPTED = false;

	Pos chunkPos;
	Region chunkRegion;
		
	public Chunk(Segment segment, int chunkNum)
	{
		if (!segment.IS_EMPTY)
		{

		}
		else
		{
			this.IS_EMPTY = true;
		}

		if (segment.IS_CORRUPTED) this.IS_CORRUPTED = true;
	}
}