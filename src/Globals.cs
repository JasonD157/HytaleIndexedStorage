namespace Globals;

public static class Globals
{
	public const int REGION_SIZE = 32; //A regionfile is 32x32 chunks
	public const int CHUNK_SIZE = 32; //A chunk is 32x32 blocks
	public const int REGION_LENGTH = REGION_SIZE * CHUNK_SIZE; //A regionfile is x blocks long.
	public const int MAX_INT = int.MaxValue;

	//Sizes/bases in bytes
	public const uint REGION_HEADER_SIZE = 32;
	public const uint SEGMENT_HEADER_SIZE = 8;

	public const uint MAGIC_SIZE = 20;
	//Magic string prepended to every region.bin file.
	public static readonly char[] MAGIC = "HytaleIndexedStorage".ToCharArray();
}

class Pos
{
	public int x;
	public int z;

	public Pos(int x, int z) { this.x = x; this.z = z; }

	public static Pos operator +(Pos self, Pos add) => new Pos(self.x + add.x, self.z + add.z);

	public override string ToString()
	{
		return $"({x},{z})";
	}
}

struct Region
{
	public Pos firstPos;
	public Pos secondPos;

	public Region(Pos first, Pos second) { this.firstPos = first; this.secondPos = second; }

	public override string ToString()
	{
		return $"{firstPos} <-> {secondPos}";
	}
}

struct ChunkHealth
{
	public int emptyChunks;
	public int filledChunks;
	public int corruptedChunks;

	public ChunkHealth(int empty, int filled, int corrupted) { this.emptyChunks = empty;  this.filledChunks = filled;  this.corruptedChunks = corrupted; }
}