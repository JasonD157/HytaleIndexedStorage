using System.Drawing;
using SegmentLib;

namespace Corruption;

enum ReservationType
{
	EMPTY,
	SEGMENT,
	REGION_HEADER,
	PAGE_CHUNK_FILLER,
}

struct ByteSpace
{
	public ulong firstByte;
	public ulong lastByte;
	public ReservationType type;
	public uint debugIndex;

	public ByteSpace(ulong first, ulong last, ReservationType type, uint debugIndex=0) { firstByte = first; lastByte = last; this.type = type; this.debugIndex = debugIndex; }

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

				newByteSpaces.Add(new ByteSpace(currB.firstByte, newB.firstByte - 1, ReservationType.EMPTY));
				newByteSpaces.Add(new ByteSpace(newB.firstByte, currB.lastByte, newType, newB.debugIndex));
			}
			else if (currB.firstByte <= newB.lastByte && currB.lastByte >= newB.lastByte) //Bytespace reaches outside current but starts in it from the right
			{
				if (currType != ReservationType.EMPTY) { Console.WriteLine(currB); Console.WriteLine(newB); throw new Exception("Tried to double-assign"); }

				newByteSpaces.Add(new ByteSpace(newB.lastByte + 1, currB.lastByte, ReservationType.EMPTY));
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

		var rest = (length - 1) % SEGMENT_SIZE;
		if (rest != 0 && type == ReservationType.SEGMENT)
		{
			ByteSpace pageFillerByteSpace = new ByteSpace(firstByte + length, firstByte + length - 1 + (SEGMENT_SIZE - rest - 1), ReservationType.PAGE_CHUNK_FILLER, index);
			MarkAsType(pageFillerByteSpace);
		}

	}

	private void SortByteSpaces()
	{
		this.byteSpaces.Sort((a, b) => a.firstByte.CompareTo(b.firstByte));
	}

	public void PrintByteSpaces()
	{
		SortByteSpaces();
		foreach (ByteSpace curr in this.byteSpaces)
		{
			if (curr.type == ReservationType.EMPTY) Console.ForegroundColor = ConsoleColor.Yellow;
			if (curr.type == ReservationType.SEGMENT) Console.ForegroundColor = ConsoleColor.Green;
			if (curr.type == ReservationType.REGION_HEADER) Console.ForegroundColor = ConsoleColor.Magenta;
			if (curr.type == ReservationType.PAGE_CHUNK_FILLER) Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(curr);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}

	public void IdentifyCorruptIndexes(uint[] validIndexes)
	{
		foreach (ByteSpace curr in this.byteSpaces)
		{
			if (curr.type != ReservationType.EMPTY) continue;

			if (curr.firstByte > long.MaxValue) throw new Exception("Number too big");

			ulong fixedIndex = (curr.firstByte - SEGMENT_BASE) / SEGMENT_SIZE + 1;

			try
			{
				_r.BaseStream.Seek((long)curr.firstByte, SeekOrigin.Begin);
				Segment trySeg = new Segment(_r);
				Console.WriteLine($"Identified index: {fixedIndex} Length: {trySeg.COMPRESSED_LENGTH}");
			}
			catch (Exception e)
			{
				Console.WriteLine($":( {String.Join(",", _r.ReadBytes(4))} {e}");
				//TODO: read Zstd header and determine if header is recoverable
			}
		}
	}
}