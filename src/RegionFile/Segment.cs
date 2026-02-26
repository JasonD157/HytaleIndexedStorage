using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Zstandard.Net;
using static BinUtil.BinHelper;
using static Globals.Globals;

namespace SegmentLib;

class Segment
{
	protected BinaryReader r;

	public uint SOURCE_LENGTH;
	public uint COMPRESSED_LENGTH;
	public byte[] uncompressedData;
	private JObject JSONObj;
	public bool IS_EMPTY = false;
	public bool IS_CORRUPTED = false;

	public Segment(BinaryReader? reader, bool IS_EMPTY=false, bool IS_CORRUPTED=false)
	{
		if (!IS_EMPTY)
		{
			this.r = reader;

			ReadHeader();
			ReadContents();
			ConvertContentsToBson();
			//We don't save the BSON results due to stack size constraints on large world files.
			//GetBSON() generates the BSON again when necessary.
		}
		else if (IS_EMPTY)
		{
			this.IS_EMPTY = IS_EMPTY;
			SOURCE_LENGTH = 0;
			COMPRESSED_LENGTH = 0;
			uncompressedData = [];
		}

		if (IS_CORRUPTED) this.IS_CORRUPTED = true;
	}

	private void ReadHeader()
	{
		this.SOURCE_LENGTH = BE(r.ReadUInt32());
		this.COMPRESSED_LENGTH = BE(r.ReadUInt32());

		if (COMPRESSED_LENGTH == 0)
			throw new InvalidDataException("Compressed Length cannot be equal to zero.");
		if (COMPRESSED_LENGTH > MAX_INT)
			//TODO: figure out if this is a valid exception (can segments be bigger than MAX_INT without corruption?)
			throw new InvalidDataException("Compressed Length cannot be bigger than int limit."); 
		if (SOURCE_LENGTH < COMPRESSED_LENGTH)
			throw new InvalidDataException("Compressed Length cannot be greater than Source Length.");
		if (r.BaseStream.Position + COMPRESSED_LENGTH > r.BaseStream.Length)
			throw new InvalidDataException("Compressed Length cannot be greater than Source Length.");
	}

	private void ReadContents()
	{
		byte[] compressedData = r.ReadBytes((int)COMPRESSED_LENGTH);

		try
		{
			using (var memoryStream = new MemoryStream(compressedData))
			using (var compressionStream = new ZstandardStream(memoryStream, System.IO.Compression.CompressionMode.Decompress))
			using (var temp = new MemoryStream())
			{
				compressionStream.CopyTo(temp);
				this.uncompressedData = temp.ToArray();
			}
		}
		catch (Exception e)
		{
			throw new InvalidDataException(e.Message, e.InnerException);
		}


		if (SOURCE_LENGTH != uncompressedData.Length)
			throw new InvalidDataException("Expected and gotten Uncompressed Sizes do not match.");
		}

	private JObject ConvertContentsToBson()
	{
		var serializer = JsonSerializer.CreateDefault();
		using (var memoryStream = new MemoryStream(uncompressedData))
		using (var bsonReader = new BsonDataReader(memoryStream))
		{
			JObject? jObject = serializer.Deserialize<JObject>(bsonReader);

			if (jObject is null)
				throw new InvalidDataException("Failed to parse BSON.");
			if (jObject.GetValue("Components") is null)
				throw new InvalidDataException("Parsed BSON does not contain Chunk[Components].");

			return jObject;
		}
	}

	public JObject GetBSON()
	{
		if (JSONObj is null) JSONObj = ConvertContentsToBson();

		return JSONObj;
	}
}