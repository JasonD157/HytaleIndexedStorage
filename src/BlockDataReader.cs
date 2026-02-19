using System.Text;
using BlockHelper;
using Globals;
using static BinUtil.BinHelper;
using static BlockHelper.BlockHelper;
using static Globals.Globals;

namespace BlockData;

struct Palette
{
	public short key;
	public string externalId;
	public uint count;

	public Palette(short key, string externalId, ushort count)
	{
		this.key = key;
		this.externalId = externalId;
		this.count = count;
	}

	public Palette(byte key, string externalId, ushort count)
	{
		this.key = key;
		this.externalId = externalId;
		this.count = count;
	}

	public override string ToString()
	{
		return $"Palette[{key} | {externalId} | {count}]";
	}
}

class BlockDataReader
{
	public int blockMigrationVersion;
	public byte paletteType;
	public int paletteLength;
	public List<Palette> palettes = new();
	public Dictionary<short, string> paletteMap = new();
	public Dictionary<Pos_3D, string> blockMap = new();

	public BlockDataReader(byte[] data, int sectionNumber, BlockChunk output)
	{
		using (var memStream = new MemoryStream(data))
		using (var reader = new BinaryReader(memStream))
		{
			ReadHeader(reader);
			ReadPalette(reader);
			ReadBlocks(reader, sectionNumber, output);
		}
	}

	private void ReadHeader(BinaryReader _r)
	{
		blockMigrationVersion = BE(_r.ReadInt32());
		paletteType = _r.ReadByte();

		if (paletteType == 0) return;

		paletteLength = BE(_r.ReadUInt16());

		//Console.WriteLine($"Version: {blockMigrationVersion} | Palette: {paletteType} | PaletteLength: {paletteLength}");
	}

	private void ReadPalette(BinaryReader _r)
	{
		for (int i = 0; i < paletteLength; i++)
		{
			short key = paletteType switch
			{
				1 => _r.ReadByte(), //(short)(_r.ReadByte() >> 4),
				2 => _r.ReadByte(),
				3 => (short)_r.ReadHalf(),
				_ => 0
			};

			uint strLength = BE(_r.ReadUInt16());
			string externalId = string.Join("", _r.ReadChars((int)strLength));
			ushort count = _r.ReadUInt16();
			var newPalette = new Palette(
				key,
				externalId,
				count
			);

			palettes.Add(newPalette);
			paletteMap.Add(key, externalId);
		}
	}

	public void ReadBlocks(BinaryReader _r, int sectionNumber, BlockChunk output)
	{
		ushort blockLength = paletteType switch
		{
			1 => 16384,
			2 => 32767,
			3 => ushort.MaxValue,
			_ => 0
		};
		ushort currentBlock = 0;

		for (int i = 0; i < blockLength; i++)
		{
			if (paletteType == 1)
			{
				byte raw = _r.ReadByte();
				byte low_key = (byte)(raw >> 4);
				byte high_key = (byte)((byte)(raw << 4) >> 4);

				string low_block = paletteMap[low_key];
				string high_block = paletteMap[high_key];

				output.AddBlock(low_block);
				output.AddBlock(high_block);
				/*output.Add(
					GetChunkPos(currentBlock) + new Pos_3D(0, sectionNumber * CHUNK_SIZE, 0),
					low_block);
				output.Add(
					GetChunkPos(currentBlock + 1) + new Pos_3D(0, sectionNumber * CHUNK_SIZE, 0),
					high_block);*/

				currentBlock += 2;

				continue;
			}
			else if (paletteType == 2)
			{
				short key = _r.ReadByte();
				output.AddBlock(paletteMap[key]);
				/*output.Add(
					GetChunkPos(currentBlock) + new Pos_3D(0, sectionNumber * CHUNK_SIZE, 0),
					paletteMap[key]);*/
			}
			else if (paletteType == 3)
			{
				short key = _r.ReadInt16();
				output.AddBlock(paletteMap[key]);
				/*output.Add(
					GetChunkPos(currentBlock) + new Pos_3D(0, sectionNumber * CHUNK_SIZE, 0),
					paletteMap[key]);*/
			}
			else
			{
				throw new Exception();
			}

			currentBlock += 1;
		}
	}
}