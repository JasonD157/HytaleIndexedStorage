using static System.Buffers.Binary.BinaryPrimitives;
using static Globals.Globals;
namespace BinUtil;

public static class BinHelper
{
	public static uint BE(uint LE)
	{
		return ReverseEndianness(LE);
	}

	public static bool IsValidSegmentHeader(BinaryReader _r, ulong startByte)
	{
		//EOF
		if ((long)startByte + SEGMENT_HEADER_SIZE >= _r.BaseStream.Length)
			return false;

		_r.BaseStream.Seek((long)startByte, SeekOrigin.Begin);
		uint SOURCE_LENGTH = BE(_r.ReadUInt32());
		uint COMPRESSED_LENGTH = BE(_r.ReadUInt32());

		if (COMPRESSED_LENGTH > MAX_INT)
			//TODO: figure out if this is a valid exception (can segments be bigger than MAX_INT without corruption?)
			return false; //throw new InvalidDataException("Compressed Length cannot be bigger than int limit."); 
		if (SOURCE_LENGTH < COMPRESSED_LENGTH)
			return false; //throw new InvalidDataException("Compressed Length cannot be greater than Source Length.");
		if (_r.BaseStream.Position + COMPRESSED_LENGTH > _r.BaseStream.Length)
			return false; //throw new InvalidDataException("Compressed Length cannot be greater than Source Length.");

		return true;
	}
}