using SegmentLib;
using static BinUtil.BinHelper;
using static Globals.Globals;

namespace Corruption;

enum ReservationType
{
	EMPTY,                  //No data, if this is present in the final dump a corrupted region lives there.
	SEGMENT,                //Chunk data
	REGION_HEADER,          //Header of the region file
	PAGE_CHUNK_FILLER,      //Hytale stores segments in BLOB_SIZES, currently 4096 bytes. As such Segments have padding.
	CORRUPT_IDX_SEGMENT,    //Broken blob_index
	CORRUPT_HDR_SEGMENT,	//Broken segment header
	CORRUPT_ZHDR_SEGMENT,   //Broken Zstd header
}

struct ByteSpace
{
	public ulong firstByte;
	public ulong lastByte;
	public ReservationType type;
	public uint debugIndex;

	public ByteSpace(ulong first, ulong last, ReservationType type, uint debugIndex=0)
	{
		firstByte = first;
		lastByte = last;
		this.type = type;
		this.debugIndex = debugIndex;

		if (lastByte-firstByte+1 == 0) throw new Exception();
	}

	public override string ToString()
	{
		return $"[{type}]".PadRight(25) + $"[{firstByte}-{lastByte}]".PadRight(25) + $"{{{lastByte-firstByte+1}}}".PadRight(10)+ $"({debugIndex})";
	}
}

class CorruptionHelper
{
	protected BinaryReader _r;
	private List<ByteSpace> byteSpaces;
	private ulong maxLength;
	private uint SEGMENT_SIZE;
	private uint SEGMENT_BASE;

	public CorruptionHelper(BinaryReader reader, ulong byteSpaceLength, uint SEGMENT_SIZE, uint SEGMENT_BASE)
	{
		this._r = reader;
		this.byteSpaces = new List<ByteSpace> { new ByteSpace(0, byteSpaceLength - 1, ReservationType.EMPTY) };
		this.maxLength = byteSpaceLength - 1;
		this.SEGMENT_SIZE = SEGMENT_SIZE;
		this.SEGMENT_BASE = SEGMENT_BASE;

		RegisterFileSpace(0, SEGMENT_BASE, 0, ReservationType.REGION_HEADER);
	}

	private void MarkAsType(ByteSpace newB)
	{
		ByteSpace match = byteSpaces.Find(b => b.firstByte <= newB.firstByte && b.lastByte >= newB.lastByte && b.type == ReservationType.EMPTY);
		if (match.Equals(default(ByteSpace)))
		{
			//Outside of file
			if (newB.lastByte > maxLength) return;

			//Double-assign due to corrupted indexes (index table contains a duplicate entry)
			if (!byteSpaces.Find(b => b.Equals(newB)).Equals(default(ByteSpace))) return;

			//Print debug info
			SortByteSpaces();
			Console.WriteLine(string.Join(",\n", byteSpaces));
			Console.WriteLine($"Curr: {newB}");
			throw new Exception("help");
		}

		byteSpaces.Remove(match);
		byteSpaces.Add(newB);

		if (match.firstByte < newB.firstByte)
		{
			byteSpaces.Add(new ByteSpace(match.firstByte, newB.firstByte - 1, match.type, match.debugIndex));
		}

		if (match.lastByte > newB.lastByte)
		{
			byteSpaces.Add(new ByteSpace(newB.lastByte + 1, match.lastByte, match.type, match.debugIndex));
		}
	}

	public ulong RegisterFileSpace(ulong firstByte, ulong length, uint index = 0, ReservationType type = ReservationType.SEGMENT)
	{
		ByteSpace segmentByteSpace = new ByteSpace(firstByte, firstByte + length - 1, type, index);
		MarkAsType(segmentByteSpace);

		//As segments are stored in SEGMENT_SIZE "pages" they have padding.
		var rest = length % SEGMENT_SIZE;
		ulong fillerLastByte = firstByte + length - 1 + 1 + (SEGMENT_SIZE - rest - 1);
		if (rest != 0 && (type == ReservationType.SEGMENT || type == ReservationType.CORRUPT_IDX_SEGMENT) && fillerLastByte <= maxLength)
		{
			ByteSpace pageFillerByteSpace = new ByteSpace(firstByte + length - 1 + 1, fillerLastByte, ReservationType.PAGE_CHUNK_FILLER, index);
			MarkAsType(pageFillerByteSpace);

			return length - 1 + 1 + (SEGMENT_SIZE - rest - 1);
		}

		return length - 1;
	}

	private void SortByteSpaces()
	{
		this.byteSpaces.Sort((a, b) => a.firstByte.CompareTo(b.firstByte));
	}

	private ReservationType GetReservationType(ulong atByte)
	{
		ByteSpace found = this.byteSpaces.FindLast(a => a.firstByte <= atByte && a.lastByte >= atByte);
		if (found.Equals(default(ByteSpace))) throw new Exception("what");

		return found.type;
	}

	public void PrintByteSpaces()
	{
		SortByteSpaces();
		foreach (ByteSpace curr in this.byteSpaces)
		{
			if (curr.type == ReservationType.EMPTY) Console.ForegroundColor = ConsoleColor.Yellow;
			if (curr.type == ReservationType.SEGMENT) Console.ForegroundColor = ConsoleColor.Green;
			if (curr.type == ReservationType.REGION_HEADER) Console.ForegroundColor = ConsoleColor.Magenta;
			if (curr.type == ReservationType.PAGE_CHUNK_FILLER) Console.ForegroundColor = ConsoleColor.Black;
			if (curr.type == ReservationType.CORRUPT_IDX_SEGMENT) Console.ForegroundColor = ConsoleColor.DarkMagenta;
			if (curr.type == ReservationType.CORRUPT_HDR_SEGMENT) Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine(curr);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}

	private bool TryRecover(ByteSpace curr, Dictionary<uint, Segment> validIndexes, bool isFinalPass)
	{
		ulong startByte = curr.firstByte;
		while (IsValidSegmentHeader(_r, startByte) && startByte <= curr.lastByte)
		{
			ulong fixedIndex = (startByte - SEGMENT_BASE) / SEGMENT_SIZE + 1;
			Segment trySeg;
			_r.BaseStream.Seek((long)startByte, SeekOrigin.Begin);
			try
			{
				trySeg = new Segment(_r);
			}
			catch
			{
				//TODO: read Zstd header and determine if header is recoverable
				ulong zHeaderSeg = RegisterFileSpace(startByte, SEGMENT_SIZE, (uint)fixedIndex, ReservationType.CORRUPT_ZHDR_SEGMENT);
				startByte += zHeaderSeg + 1;
				continue;
			}

			ulong bytesClaimed = RegisterFileSpace(startByte, trySeg.COMPRESSED_LENGTH, (uint)fixedIndex, ReservationType.CORRUPT_IDX_SEGMENT); //Padding incl.
			validIndexes.Add((uint)fixedIndex, trySeg);
			startByte += bytesClaimed + 1;
		}

		return true;
	}

	public bool AssignCorruptedHeaders()
	{
		bool assignedHeaders = false;
		foreach (ByteSpace curr in new List<ByteSpace>(byteSpaces.FindAll(b => b.type == ReservationType.EMPTY)))
		{
			if (curr.firstByte > long.MaxValue) throw new Exception("Number too big");
			ulong currByte = curr.firstByte;

			while (!IsValidSegmentHeader(_r, currByte))
			{
				ulong fixedIndex = (currByte - SEGMENT_BASE) / SEGMENT_SIZE + 1;
				if (currByte + SEGMENT_SIZE <= maxLength)
				{
					RegisterFileSpace(currByte, SEGMENT_SIZE, (uint)fixedIndex, ReservationType.CORRUPT_HDR_SEGMENT);
					assignedHeaders = true;
					currByte += SEGMENT_SIZE;
				}
				else
				{
					break;
				}
			}
		}

		return assignedHeaders;
	}

	public Dictionary<uint, Segment> IdentifyCorruptedSegments()
	{
		Dictionary<uint, Segment> validIndexes = new();

		do
		{
			IdentifyCorruptIndexes(validIndexes);

		}
		while (AssignCorruptedHeaders());

		return validIndexes;
	}

	private bool IdentifyCorruptIndexes(Dictionary<uint, Segment> validIndexes, bool isFinalPass=false)
	{
		bool byteSpacesWasUpdated = false;
		foreach (ByteSpace curr in new List<ByteSpace>(byteSpaces.FindAll(b => b.type == ReservationType.EMPTY)))
		{
			if (curr.firstByte > long.MaxValue) throw new Exception("Number too big");

			byteSpacesWasUpdated = TryRecover(curr, validIndexes, isFinalPass);
		}

		//if (byteSpacesWasUpdated) IdentifyCorruptIndexes(validIndexes);

		return byteSpacesWasUpdated;
	}
}