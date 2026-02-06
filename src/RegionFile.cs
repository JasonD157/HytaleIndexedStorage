using SegmentLib;
using ChunkLib;
using Globals;
using static System.Diagnostics.Debug;
using static BinUtil.BinHelper;
using static Globals.Globals;
using Corruption;
using Newtonsoft.Json;

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

class RegionFile
{
	protected BinaryReader _r;
	protected CorruptionHelper _corruptionHelper;

	private uint _VERSION;
	private uint _BLOB_COUNT;
	private uint _SEGMENT_SIZE;
	private uint _SEGMENT_BASE;
	private uint[] _blob_indexes;

	public Segment[] rawSegments;
	public Chunk[] chunks;

	public string fileName;
	public Pos regionPos;
	public Region chunkRegion;

	public ChunkHealth chunkHealth;

	//Debug flags
	private bool _INCLUDE_RAW_SEGMENTS;
	

	public RegionFile(BinaryReader reader, string fileName, bool INCLUDE_RAW_SEGMENTS = false)
	{
		this._r = reader;
		this.fileName = fileName;
		this._INCLUDE_RAW_SEGMENTS = INCLUDE_RAW_SEGMENTS;

		CalculateRegionInfo();
		ReadHeader();
		ReadBlobIndexes();

		this._corruptionHelper = new CorruptionHelper(reader, (ulong)reader.BaseStream.Length, _SEGMENT_SIZE, _SEGMENT_BASE);
		
		ReadBlobs();
		ConvertBlobsToChunks();
		CalculateRegionStatistics();
	}

	private void CalculateRegionInfo()
	{
		string[] splits = fileName.Split(".");
		int r_x, r_z = 0;
		Assert(int.TryParse(splits[^3], out r_x) && int.TryParse(splits[^4], out r_z), "Couldn't parse region coordinates from filename.");

		Pos innerPos = new Pos(r_x * REGION_LENGTH, r_z * REGION_LENGTH);
		Pos outerPos = new Pos((r_x + 1) * REGION_LENGTH, (r_z + 1) * REGION_LENGTH);

		this.regionPos = innerPos;
		this.chunkRegion = new Region(innerPos, outerPos);
	}

	private void ReadHeader()
	{
		//Can't use direct compare here as the objects are not the same.
		Assert(_r.ReadChars((int)MAGIC_SIZE).SequenceEqual(MAGIC), "Magic does not match. Is the supplied file a region.bin file?");

		this._VERSION = BE(_r.ReadUInt32());
		this._BLOB_COUNT = BE(_r.ReadUInt32());
		this._SEGMENT_SIZE = BE(_r.ReadUInt32());
	}

	private void ReadBlobIndexes()
	{
		uint[] indexes = new uint[_BLOB_COUNT];
		for (int i = 0; i < _BLOB_COUNT; i++)
		{
			indexes[i] = BE(_r.ReadUInt32());
		}

		this._SEGMENT_BASE = REGION_HEADER_SIZE + (_BLOB_COUNT * 4); //4 bytes per index (UInt32), blob_count indexes.
		this._blob_indexes = indexes;
	}

	private void ReadBlobs()
	{
		Segment[] segments = new Segment[_BLOB_COUNT];
		uint[] validIndexes = new uint[_BLOB_COUNT];
		for (int i = 0; i < _BLOB_COUNT; i++)
		{
			uint index = _blob_indexes[i];

			if (index == 0) //Empty Blobs are indicated by a '0' index.
			{
				segments[i] = new Segment(null, IS_EMPTY: true);
				continue;
			}

			(bool success, segments[i]) = ReadBlob(index);
			if (success) validIndexes[i] = index;
		}

		this.rawSegments = segments;
		_corruptionHelper.PrintByteSpaces();
		_corruptionHelper.IdentifyCorruptIndexes(validIndexes);
	}

	private (bool, Segment) ReadBlob(uint index)
	{
		ulong SEGMENT_POS = (ulong)_SEGMENT_BASE + (index - 1) * _SEGMENT_SIZE;

		try
		{
			_r.BaseStream.Seek((long)SEGMENT_POS, SeekOrigin.Begin);
			Segment newSegment = new Segment(_r);
			_corruptionHelper.RegisterFileSpace(SEGMENT_POS, COMPRESSED_HEADER_SIZE + newSegment.COMPRESSED_LENGTH, index);
			return (true, newSegment);
		}
		catch (InvalidDataException e) //Chunk corruption
		{
			Console.WriteLine($"[WARNING]:\tChunk at index {index} in regionfile {fileName} is corrupted!\n\t\t({e.Message})");
			return (false, new Segment(null, IS_EMPTY: true, IS_CORRUPTED: true));
		}
		catch (Exception e) //Unexpected Exception type (probs from _corruptionHelper)
		{
			throw new Exception(e.StackTrace);
		}
	}

	private void ConvertBlobsToChunks()
	{
		Chunk[] chunks = new Chunk[_BLOB_COUNT];
		for (int i = 0; i < _BLOB_COUNT; i++)
		{
			Segment segment = rawSegments[i];
			chunks[i] = new Chunk(segment, this.chunkRegion, i);

			string json = JsonConvert.SerializeObject(segment.JSONObj);
			File.WriteAllText($"../../../jsondump/jsondump_{regionPos.x}_{regionPos.z}_{chunks[i].chunkPos.x}_{chunks[i].chunkPos.z}.temp.json", json);
		}

		this.chunks = chunks;
	}

	private void CalculateRegionStatistics()
	{
		int emptyChunks = 0;
		int filledChunks = 0;
		int corruptedChunks = 0;

		for (int i = 0; i < _BLOB_COUNT; i++)
		{
			Chunk chunk = chunks[i];

			if (chunk.IS_CORRUPTED)
			{
				corruptedChunks += 1;
			}
			else if (chunk.IS_EMPTY)
			{
				emptyChunks += 1;
			}
			else
			{
				filledChunks += 1;
			}
		}

		this.chunkHealth = new ChunkHealth(emptyChunks, filledChunks, corruptedChunks);
	}
}