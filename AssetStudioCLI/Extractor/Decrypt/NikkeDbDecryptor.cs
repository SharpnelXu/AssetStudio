using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace AssetStudioCLI.Extractor.Decrypt;

/**
 * Author: Hiro420 (https://github.com/Hiro420/NikkeTools)
 */
class NikkeDbDecryptor
{
	public static FileInfo DecryptNikkeDatabase(NikkeExtractorOptions options)
	{
		if (!File.Exists(options.DbPath))
		{
			throw new FileNotFoundException("Nikke database not found", options.DbPath);
		}
		
		using var fs = new FileStream(options.DbPath, FileMode.Open, FileAccess.Read);
		using var reader = new BinaryReader(fs);

		var header = new NikkeDatabaseHeader
		{
			Magic = reader.ReadBytes(4),
			Version = ReadUInt32BigEndian(reader),
			AesKey = reader.ReadBytes(16),
			SegmentSize = ReadUInt32BigEndian(reader),
			SegmentCount = ReadUInt32BigEndian(reader)
		};

		if (!header.Magic.SequenceEqual("NKDB"u8.ToArray()))
			throw new ArgumentException("Invalid DB header" + header.Magic); // invalid magic

		if (header.Version != 1)
			throw new ArgumentException("Invalid DB version" + header.Version); // invalid version

		long ReadOffset()
		{
			byte[] offsetBytes = reader.ReadBytes(4);
			return offsetBytes.Aggregate(0L, (acc, b) => (acc << 8) | b);
		}

		long currentOffset = ReadOffset();
		var segments = new (long Offset, long Length, int Index)[header.SegmentCount];

		for (int i = 0; i < header.SegmentCount; i++)
		{
			long nextOffset = ReadOffset();
			segments[i] = (currentOffset, nextOffset - currentOffset, i);
			currentOffset = nextOffset;
		}

		var outputDirectory = options.StoreDbOutputPath ?? Path.Combine(".", "tmp");
		Directory.CreateDirectory(outputDirectory);
		var file = new FileInfo(options.DbPath);
		var outputFilePath = Path.Combine(outputDirectory, file.Name);
		using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

		foreach (var (offset, length, index) in segments)
		{
			fs.Seek(offset, SeekOrigin.Begin);
			byte[] segment = reader.ReadBytes((int)length);

			byte[] iv = new byte[16];
			BitConverter.GetBytes(index).CopyTo(iv, 0);
			BitConverter.GetBytes((int)offset).CopyTo(iv, 4);

			byte[] decrypted = DecryptAES_OFB(header.AesKey, iv, segment);

			using var ms = new MemoryStream(decrypted);
			using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
			zlib.CopyTo(outputStream);
		}
		
		return new FileInfo(outputFilePath);
	}

	static uint ReadUInt32BigEndian(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian)
			Array.Reverse(bytes);
		return BitConverter.ToUInt32(bytes, 0);
	}

	static byte[] DecryptAES_OFB(byte[] key, byte[] iv, byte[] input)
	{
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = iv;
		MemoryStream testVectorStream = new MemoryStream(input);
		OFBStream testOFBStream = new OFBStream(testVectorStream, aes, CryptoStreamMode.Read);
		MemoryStream cipherTextStream = new MemoryStream();
		testOFBStream.CopyTo(cipherTextStream);
		return cipherTextStream.ToArray();
	}
}

struct NikkeDatabaseHeader
{
	public byte[] Magic;
	public uint Version;
	public byte[] AesKey;
	public uint SegmentSize;
	public uint SegmentCount;
}