using System.Reflection;
using static System.Diagnostics.Debug;
using SegmentLib;
using ChunkLib;
using static BinUtil.BinHelper;
using System.Buffers.Binary;

namespace RegionFileLib;

class RegionFileUtil
{
	private BinaryReader reader;
	string fileName;

	public RegionFileUtil(BinaryReader reader, string fileName)
	{
		this.reader = reader;
		this.fileName = fileName;
	}

	public RegionFile HandleRegionFile()
	{
		return new RegionFile(reader, fileName);
	}
}

class Pos
{
	public int x;
	public int z;

	public Pos(int x, int z) { this.x = x; this.z = z; }

	public Pos Add(Pos pos1, Pos pos2)
	{
		return new Pos(pos1.x + pos2.x, pos1.z + pos2.z);
	}
}

struct Region
{
	public Pos firstPos;
	public Pos secondPos;

	public Region(Pos first, Pos second) { this.firstPos = first; this.secondPos = second; }
}

class RegionFile
{
	protected BinaryReader r;

	public uint VERSION;
	public uint BLOB_COUNT;
	public uint SEGMENT_SIZE;
	public uint[] blob_indexes;

	public Segment[] rawSegments;
	public Chunk[] chunks;

	public string fileName;
	public Pos regionPos;
	public Region chunkRegion;

	public int emptyChunks = 1024;
	public int filledChunks = 0;
	public int corruptedChunks = 0;

	//Debug flags
	private bool INCLUDE_RAW_SEGMENTS;
	//Sizes/bases in bytes
	private int HEADER_SIZE = 32;
	private int SEGMENT_BASE;
	private int MAGIC_SIZE = 20;
	//Magic string prepended to every region.bin file.
	private char[] MAGIC = "HytaleIndexedStorage".ToCharArray();

	public RegionFile(BinaryReader reader, string fileName, bool INCLUDE_RAW_SEGMENTS=false)
	{
		this.r = reader;
		this.fileName = fileName;
		this.INCLUDE_RAW_SEGMENTS = INCLUDE_RAW_SEGMENTS;

		CalculateRegionInfo();
		ReadHeader();
		ReadBlobIndexes();
		ReadBlobs();
		ConvertBlobsToChunks();
		CalculateRegionStatistics();
	}

	private void CalculateRegionInfo()
	{
		string[] splits = fileName.Split(".");
		int r_x, r_z = 0;
		Assert(int.TryParse(splits[^3], out r_x) && int.TryParse(splits[^4], out r_z), "Couldn't parse region coordinates from filename.");

		Pos innerPos = new Pos(r_x, r_z);
		Pos outerPos = new Pos((r_x + 1) * 32, (r_z + 1) * 32);

		this.regionPos = innerPos;
		this.chunkRegion = new Region(innerPos, outerPos);
	}

	private void ReadHeader()
	{
		//Can't use direct compare here as the objects are not the same.
		Assert(r.ReadChars(MAGIC_SIZE).SequenceEqual(MAGIC), "Magic does not match. Is the supplied file a region.bin file?");

		this.VERSION = BE(r.ReadUInt32());
		this.BLOB_COUNT = BE(r.ReadUInt32());
		this.SEGMENT_SIZE = BE(r.ReadUInt32());
	}

	private void ReadBlobIndexes()
	{
		uint[] indexes = new uint[BLOB_COUNT];
		for (int i = 0; i < BLOB_COUNT; i++)
		{
			indexes[i] = BE(r.ReadUInt32());
		}

		this.SEGMENT_BASE = HEADER_SIZE + ((int)BLOB_COUNT * 4); //4 bytes per index (UInt32), blob_count indexes.
		this.blob_indexes = indexes;
	}

	private void ReadBlobs()
	{
		Segment[] segments = new Segment[BLOB_COUNT];
		for (int i = 0; i < BLOB_COUNT; i++)
		{
			uint index = blob_indexes[i];

			if (index == 0) //Empty Blobs are indicated by a '0' index.
			{
				segments[i] = new Segment(null, IS_EMPTY: true);
				continue;
			}

			segments[i] = ReadBlob(index);
		}

		this.rawSegments = segments;
	}

	private Segment ReadBlob(uint index)
	{
		int SEGMENT_POS = SEGMENT_BASE + ((int)index - 1) * (int)SEGMENT_SIZE;
		
		try
		{
			r.BaseStream.Seek(SEGMENT_POS, SeekOrigin.Begin);
			return new Segment(r);
		}
		catch (Exception e)
		{
			Console.WriteLine($"[WARNING]:\tChunk at index {index} in regionfile {fileName} is corrupted!\n\t\t({e.Message})");
			return new Segment(null, IS_EMPTY: true, IS_CORRUPTED: true);
		}
	}

	private void ConvertBlobsToChunks()
	{
		Chunk[] chunks = new Chunk[BLOB_COUNT];
		for (int i = 0; i < BLOB_COUNT; i++)
		{
			Segment segment = rawSegments[i];
			chunks[i] = new Chunk(segment, this.chunkRegion, i);
		}

		this.chunks = chunks;
	}

	private void CalculateRegionStatistics()
	{
		for (int i = 0; i < BLOB_COUNT; i++)
		{
			Chunk chunk = chunks[i];

			if (chunk.IS_CORRUPTED)
			{
				emptyChunks -= 1;
				corruptedChunks += 1;
			}
			else if (chunk.IS_EMPTY)
			{
				//Nothing needs to be done as we assume all chunks are empty at init.
			}
			else
			{
				emptyChunks -= 1;
				filledChunks += 1;
			}
		}
	}
}