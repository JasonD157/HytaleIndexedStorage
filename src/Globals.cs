namespace Globals;

public static class Globals
{
	public const int REGION_SIZE = 32; //A regionfile is 32x32 chunks
	public const int CHUNK_SIZE = 32; //A chunk is 32x32 blocks
	public const int CHUNK_AREA = CHUNK_SIZE * CHUNK_SIZE; // 32x32
	public const int CHUNK_SECTIONS = 10; //A chunk is 10 sections of 32x32x32 high, resulting in a 32x32x320 chunk
	public const int CHUNK_HEIGHT = CHUNK_SECTIONS * CHUNK_SIZE;
	public const int REGION_LENGTH = REGION_SIZE * CHUNK_SIZE; //A regionfile is x blocks long.
	public const int MAX_INT = int.MaxValue;

	public const string Air_BlockName = "Empty";

	//Sizes/bases in bytes
	public const uint REGION_HEADER_SIZE = 32;
	public const uint SEGMENT_HEADER_SIZE = 8;

	public const uint MAGIC_SIZE = 20;
	//Magic string prepended to every region.bin file.
	public static readonly char[] MAGIC = "HytaleIndexedStorage".ToCharArray();
}

class Pos_2D
{
	public int x;
	public int z;

	public Pos_2D(int x, int z) { this.x = x; this.z = z; }

	public static Pos_2D operator +(Pos_2D self, Pos_2D add) => new Pos_2D(self.x + add.x, self.z + add.z);

	public override string ToString()
	{
		return $"({x},{z})";
	}
}

class Pos_3D
{
	public int x;
	public int y;
	public int z;

	public Pos_3D(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }

	public static Pos_3D operator +(Pos_3D self, Pos_3D add) => new Pos_3D(self.x + add.x, self.y + add.y, self.z + add.z);

	public override string ToString()
	{
		return $"({x},{y},{z})";
	}
}

struct Region
{
	public Pos_2D firstPos;
	public Pos_2D secondPos;

	public Region(Pos_2D first, Pos_2D second) { this.firstPos = first; this.secondPos = second; }

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