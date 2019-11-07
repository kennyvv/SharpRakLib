using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NLog;

namespace SharpRakLib.Core
{
    public class Compression
	{
		public static byte[] Compress(Memory<byte> input, bool writeLen = false, CompressionLevel compressionLevel = CompressionLevel.Fastest)
		{
			return CompressIntoStream(input, compressionLevel, writeLen).ToArray();
		}

		public static MemoryStream CompressIntoStream(Memory<byte> input, CompressionLevel compressionLevel, bool writeLen = false)
		{
			var stream = new MemoryStream();

			stream.WriteByte(0x78);
			switch (compressionLevel)
			{
				case CompressionLevel.Optimal:
					stream.WriteByte(0xda);
					break;
				case CompressionLevel.Fastest:
					stream.WriteByte(0x9c);
					break;
				case CompressionLevel.NoCompression:
					stream.WriteByte(0x01);
					break;
			}
			int checksum = 0;
			using (var compressStream = new ZLibStream(stream, compressionLevel, true))
			{
				if (writeLen)
				{
					WriteLength(compressStream, input.Length);
				}

				compressStream.Write(input.Span);
				checksum = compressStream.Checksum;
			}

			var checksumBytes = BitConverter.GetBytes(checksum);
			if (BitConverter.IsLittleEndian)
			{
				// Adler32 checksum is big-endian
				Array.Reverse(checksumBytes);
			}
			stream.Write(checksumBytes, 0, checksumBytes.Length);
			return stream;
		}

		public static void WriteLength(Stream stream, int lenght)
		{
			VarInt.WriteUInt32(stream, (uint) lenght);
		}

		public static int ReadLength(Stream stream)
		{
			return (int) VarInt.ReadUInt32(stream);
		}

		/*public static byte[] Decompress(byte[] buffer)
		{
			MemoryStream stream = new MemoryStream(buffer);
			if (stream.ReadByte() != 0x78)
			{
				throw new InvalidDataException("Incorrect ZLib header. Expected 0x78 0x9C");
			}
			stream.ReadByte();
			using (var defStream2 = new DeflateStream(stream, CompressionMode.Decompress, false))
			{
				// Get actual package out of bytes
				MemoryStream destination = new MemoryStream();
				defStream2.CopyTo(destination);
				destination.Position = 0;
				NbtBinaryReader reader = new NbtBinaryReader(destination, true);
				var len = ReadLength(destination);
				byte[] internalBuffer = reader.ReadBytes(len);

				//Log.Debug($"Package [len={len}:\n" + Package.HexDump(internalBuffer));

				if (destination.Length > destination.Position) throw new Exception($"Read {len} bytes, but have more data. Length={destination.Length}, Pos={destination.Position}");

				return internalBuffer;
			}
		}*/
	}
    
    public sealed class ZLibStream : DeflateStream
    {
	    private uint _adler = 1;

	    private const int ChecksumModulus = 65521;

	    public int Checksum => (int) _adler;

	    private uint Update(uint adler, byte[] s, int offset, int count)
	    {
		    uint l = (ushort) adler;
		    ulong h = adler >> 16;
		    int p = 0;
		    for (; p < (count & 7); ++p)
			    h += (l += s[offset + p]);

		    for (; p < count; p += 8)
		    {
			    var idx = offset + p;
			    h += (l += s[idx]);
			    h += (l += s[idx + 1]);
			    h += (l += s[idx + 2]);
			    h += (l += s[idx + 3]);
			    h += (l += s[idx + 4]);
			    h += (l += s[idx + 5]);
			    h += (l += s[idx + 6]);
			    h += (l += s[idx + 7]);
		    }

		    return (uint) (((h % ChecksumModulus) << 16) | (l % ChecksumModulus));
	    }

	    public ZLibStream(Stream stream, CompressionLevel level, bool leaveOpen) : base(stream, level, leaveOpen)
	    {
	    }

	    public override void Write(byte[] array, int offset, int count)
	    {
		    _adler = Update(_adler, array, offset, count);
		    base.Write(array, offset, count);
	    }
    }
    
    public static class VarInt
	{
		private static uint EncodeZigZag32(int n)
		{
			// Note:  the right-shift must be arithmetic
			return (uint) ((n << 1) ^ (n >> 31));
		}

		private static int DecodeZigZag32(uint n)
		{
			return (int) (n >> 1) ^ -(int) (n & 1);
		}

		private static ulong EncodeZigZag64(long n)
		{
			return (ulong) ((n << 1) ^ (n >> 63));
		}

		private static long DecodeZigZag64(ulong n)
		{
			return (long) (n >> 1) ^ -(long) (n & 1);
		}

		private static uint ReadRawVarInt32(Stream buf, int maxSize)
		{
			uint result = 0;
			int j = 0;
			int b0;

			do
			{
				b0 = buf.ReadByte(); // -1 if EOS
				if (b0 < 0) throw new EndOfStreamException("Not enough bytes for VarInt");

				result |= (uint) (b0 & 0x7f) << j++ * 7;

				if (j > maxSize)
				{
					throw new OverflowException("VarInt too big");
				}
			} while ((b0 & 0x80) == 0x80);

			return result;
		}

		private static ulong ReadRawVarInt64(Stream buf, int maxSize, bool printBytes = false)
		{
			List<byte> bytes = new List<byte>();

			ulong result = 0;
			int j = 0;
			int b0;

			do
			{
				b0 = buf.ReadByte(); // -1 if EOS
				bytes.Add((byte) b0);
				if (b0 < 0) throw new EndOfStreamException("Not enough bytes for VarInt");

				result |= (ulong) (b0 & 0x7f) << j++ * 7;

				if (j > maxSize)
				{
					throw new OverflowException("VarInt too big");
				}
			} while ((b0 & 0x80) == 0x80);

			byte[] byteArray = bytes.ToArray();

			return result;
		}

		private static void WriteRawVarInt32(Stream buf, uint value)
		{
			while ((value & -128) != 0)
			{
				buf.WriteByte((byte) ((value & 0x7F) | 0x80));
				value >>= 7;
			}

			buf.WriteByte((byte) value);
		}

		private static void WriteRawVarInt64(Stream buf, ulong value)
		{
			while ((value & 0xFFFFFFFFFFFFFF80) != 0)
			{
				buf.WriteByte((byte) ((value & 0x7F) | 0x80));
				value >>= 7;
			}

			buf.WriteByte((byte) value);
		}

		// Int

		public static void WriteInt32(Stream stream, int value)
		{
			WriteRawVarInt32(stream, (uint) value);
		}

		public static int ReadInt32(Stream stream)
		{
			return (int) ReadRawVarInt32(stream, 5);
		}

		public static void WriteSInt32(Stream stream, int value)
		{
			WriteRawVarInt32(stream, EncodeZigZag32(value));
		}

		public static int ReadSInt32(Stream stream)
		{
			return DecodeZigZag32(ReadRawVarInt32(stream, 5));
		}

		public static void WriteUInt32(Stream stream, uint value)
		{
			WriteRawVarInt32(stream, value);
		}

		public static uint ReadUInt32(Stream stream)
		{
			return ReadRawVarInt32(stream, 5);
		}

		// Long

		public static void WriteInt64(Stream stream, long value)
		{
			WriteRawVarInt64(stream, (ulong) value);
		}

		public static long ReadInt64(Stream stream, bool printBytes = false)
		{
			return (long) ReadRawVarInt64(stream, 10, printBytes);
		}

		public static void WriteSInt64(Stream stream, long value)
		{
			WriteRawVarInt64(stream, EncodeZigZag64(value));
		}

		public static long ReadSInt64(Stream stream)
		{
			return DecodeZigZag64(ReadRawVarInt64(stream, 10));
		}

		public static void WriteUInt64(Stream stream, ulong value)
		{
			WriteRawVarInt64(stream, value);
		}

		public static ulong ReadUInt64(Stream stream)
		{
			return ReadRawVarInt64(stream, 10);
		}
	}
}