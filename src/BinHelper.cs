using static System.Buffers.Binary.BinaryPrimitives;
namespace BinUtil;

public static class BinHelper
{
	public static uint BE(uint LE)
	{
		return ReverseEndianness(LE);
	}
}