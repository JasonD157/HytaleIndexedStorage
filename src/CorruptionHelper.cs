using System.Drawing;
using SegmentLib;

namespace Corruption;

enum ReservationType
{
	EMPTY,					//No data, if this is present in the final dump a corrupted region lives there.
	SEGMENT,				//Chunk data
	REGION_HEADER,			//Header of the region file
	PAGE_CHUNK_FILLER,		//Hytale stores segments in BLOB_SIZES, currently 4096 bytes. As such Segments have padding.
	CORRUPT_IDX_SEGMENT, 	//Broken blob_index
	CORRUPT_HDR_SEGMENT, 	//Broken Zstd header
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
	private uint SEGMENT_SIZE;
	private uint SEGMENT_BASE;

	public CorruptionHelper(BinaryReader reader, ulong byteSpaceLength, uint SEGMENT_SIZE, uint SEGMENT_BASE)
	{
		this._r = reader;
		this.byteSpaces = new List<ByteSpace> { new ByteSpace(0, byteSpaceLength - 1, ReservationType.EMPTY) };
		this.SEGMENT_SIZE = SEGMENT_SIZE;
		this.SEGMENT_BASE = SEGMENT_BASE;

		RegisterFileSpace(0, SEGMENT_BASE, 0, ReservationType.REGION_HEADER);
	}

	private void MarkAsType(ByteSpace newB)
	{
		List<ByteSpace> newByteSpaces = new();
		//Console.WriteLine($"DebugSegment: {segment}");

		foreach (ByteSpace currB in this.byteSpaces)
		{
			//Console.WriteLine($"DebugCurr: {curr}");
			ReservationType currType = currB.type;
			ReservationType newType = newB.type;

			if (newB.Equals(currB)) continue; //Index Corruption causing double assignment TODO: handle properly
			
			if (currB.firstByte <= newB.firstByte && currB.lastByte >= newB.lastByte) //Bytespace is within
			{
				if (currType != ReservationType.EMPTY) { Console.WriteLine(currB); Console.WriteLine(newB); throw new Exception("Tried to double-assign"); }

				newByteSpaces.Add(newB);

				if (currB.firstByte == newB.firstByte && currB.lastByte == newB.lastByte)
				{
					continue;
				}
				else if (currB.firstByte == newB.firstByte)
				{
					newByteSpaces.Add(new ByteSpace(newB.lastByte + 1, currB.lastByte, ReservationType.EMPTY));
				}
				else if (currB.lastByte == newB.lastByte)
				{
					newByteSpaces.Add(new ByteSpace(currB.firstByte, newB.firstByte - 1, ReservationType.EMPTY));
				}
				else
				{
					newByteSpaces.Add(new ByteSpace(currB.firstByte, newB.firstByte - 1, ReservationType.EMPTY));
					newByteSpaces.Add(new ByteSpace(newB.lastByte + 1, currB.lastByte, ReservationType.EMPTY));
				}
			}
			else if (currB.firstByte <= newB.firstByte && currB.lastByte >= newB.firstByte) //Bytespace reaches outside current but starts in it from the left
			{
				if (currType != ReservationType.EMPTY) { Console.WriteLine(currB); Console.WriteLine(newB); throw new Exception("Tried to double-assign"); }

				if (currB.firstByte != newB.firstByte)
				{
					newByteSpaces.Add(new ByteSpace(currB.firstByte, newB.firstByte - 1, ReservationType.EMPTY));
				}
				newByteSpaces.Add(new ByteSpace(newB.firstByte, currB.lastByte, newType, newB.debugIndex));
			}
			else if (currB.firstByte <= newB.lastByte && currB.lastByte >= newB.lastByte) //Bytespace reaches outside current but starts in it from the right
			{
				if (currType != ReservationType.EMPTY) { Console.WriteLine(currB); Console.WriteLine(newB); throw new Exception("Tried to double-assign"); }

				if (newB.lastByte != currB.lastByte)
				{
					newByteSpaces.Add(new ByteSpace(newB.lastByte + 1, currB.lastByte, ReservationType.EMPTY));
				}
				newByteSpaces.Add(new ByteSpace(currB.firstByte, newB.lastByte, newType, newB.debugIndex));
			}
			else //Bytespace reaches completely outside current
			{
				newByteSpaces.Add(currB);
			}
		}

		this.byteSpaces = newByteSpaces;
	}

	public void RegisterFileSpace(ulong firstByte, ulong length, uint index = 0, ReservationType type = ReservationType.SEGMENT)
	{
		ByteSpace segmentByteSpace = new ByteSpace(firstByte, firstByte + length - 1, type, index);
		MarkAsType(segmentByteSpace);

		//As segments are stored in SEGMENT_SIZE "pages" they have padding.
		var rest = (length - 1) % SEGMENT_SIZE;
		if (rest != 0 && (type == ReservationType.SEGMENT || type == ReservationType.CORRUPT_IDX_SEGMENT))
		{
			ByteSpace pageFillerByteSpace = new ByteSpace(firstByte + length, firstByte + length - 1 + (SEGMENT_SIZE - rest - 1), ReservationType.PAGE_CHUNK_FILLER, index);
			MarkAsType(pageFillerByteSpace);
		}
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

	private bool TryRecover(ulong startByte, Dictionary<uint, Segment> validIndexes, bool isFinalPass)
	{
		ulong fixedIndex = (startByte - SEGMENT_BASE) / SEGMENT_SIZE + 1;

		try
		{
			_r.BaseStream.Seek((long)startByte, SeekOrigin.Begin);
			Segment trySeg = new Segment(_r);
			RegisterFileSpace(startByte, trySeg.COMPRESSED_LENGTH, (uint)fixedIndex, ReservationType.CORRUPT_IDX_SEGMENT);
			validIndexes.Add((uint)fixedIndex, trySeg);
			return true;
		}
		catch
		{
			return false;
			//TODO: read Zstd header and determine if header is recoverable
		}
	}

	public bool AssignCorruptedHeaders()
	{
		bool assignedHeaders = false;
		foreach (ByteSpace curr in this.byteSpaces)
		{
			if (curr.type != ReservationType.EMPTY) continue;
			if (curr.firstByte > long.MaxValue) throw new Exception("Number too big");

			ulong fixedIndex = (curr.firstByte - SEGMENT_BASE) / SEGMENT_SIZE + 1;
			RegisterFileSpace(curr.firstByte, SEGMENT_SIZE, (uint)fixedIndex, ReservationType.CORRUPT_HDR_SEGMENT);
			assignedHeaders = true;
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
		foreach (ByteSpace curr in this.byteSpaces)
		{
			if (curr.type != ReservationType.EMPTY) continue;

			if (curr.firstByte > long.MaxValue) throw new Exception("Number too big");

			byteSpacesWasUpdated = TryRecover(curr.firstByte, validIndexes, isFinalPass);
		}

		if (byteSpacesWasUpdated) IdentifyCorruptIndexes(validIndexes);

		return byteSpacesWasUpdated;
	}
}