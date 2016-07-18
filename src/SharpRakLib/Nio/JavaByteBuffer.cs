using System;
using System.IO;
using System.Linq;
using System.Text;
using SharpRakLib.Util;

namespace SharpRakLib.Nio
{
	public class JavaByteBuffer : IBuffer
	{
		private readonly MemoryStream _buffer;

		public JavaByteBuffer(MemoryStream buffer)
		{
			this._buffer = buffer;
		}

		public byte[] Get(int len)
		{
			var data = new byte[len];
			_buffer.Read(data, 0, data.Length);
			Array.Reverse(data);
			return data;
		}

		public void Put(byte[] bytes)
		{
			//Array.Reverse(bytes);
			_buffer.Write(bytes, 0, bytes.Length);
		}

		public byte GetByte()
		{
			return (byte) _buffer.ReadByte();
		}

		public short GetShort()
		{
			return BitConverter.ToInt16(Get(2), 0);
		}

		public ushort GetUnsignedShort()
		{
			return BitConverter.ToUInt16(Get(2), 0);
		}

		public int GetLTriad()
		{
			return (GetByte() & 0xFF) | ((GetByte() & 0xFF) << 8) | ((GetByte() & 0x0F) << 16);
		}

		public int GetInt()
		{
			return BitConverter.ToInt32(Get(4), 0);
		}

		public long GetLong()
		{
			return BitConverter.ToInt64(Get(8), 0);
		}

		public string GetString()
		{
			return Encoding.UTF8.GetString(Get(GetUnsignedShort()));
		}

		public SystemAddress GetAddress()
		{
			int version = GetByte();
			if (version == 4)
			{
				var address = (~GetByte() & 0xff) + "." + (~GetByte() & 0xff) + "." + (~GetByte() & 0xff) + "." +
				              (~GetByte() & 0xff);
				int port = GetUnsignedShort();
				return new SystemAddress(address, port, version);
			}
			if (version == 6)
			{
				//TODO: IPv6 Decode
				throw new Exception("Can't read IPv6 address: Not Implemented");
			}
			throw new Exception("Can't read IPv" + version + " address: unknown");
		}

		public bool GetBoolean()
		{
			return GetByte() > 0;
		}

		public void PutByte(byte b)
		{
			_buffer.WriteByte(b);
		}

		public void PutShort(short s)
		{
			Put(BitConverter.GetBytes(Endian.SwapInt16(s)));
			//_buffer.Write(BitConverter.GetBytes(s), 0, 2);
		}

		public void PutUnsignedShort(ushort us)
		{
			Put(BitConverter.GetBytes(Endian.SwapUInt16(us)));
			//_buffer.Write(BitConverter.GetBytes(us), 0, 2);
		}

		public void PutLTriad(int t)
		{
			byte b1, b2, b3;
			b3 = (byte) (t & 0xFF);
			b2 = (byte) ((t >> 8) & 0xFF);
			b1 = (byte) ((t >> 16) & 0xFF);
			Put(new[] {b3, b2, b1});
		}

		public void PutInt(int i)
		{
			Put(BitConverter.GetBytes(Endian.SwapInt32(i)));
			//_buffer.Write(BitConverter.GetBytes(i), 0, 3);
		}

		public void PutLong(long l)
		{
			Put(BitConverter.GetBytes(Endian.SwapInt64(l)));
			//_buffer.Write(BitConverter.GetBytes(l), 0, 8);
		}

		public void PutString(string s)
		{
			var bytes = Encoding.UTF8.GetBytes(s);
			PutUnsignedShort((ushort) bytes.Length);
			Put(bytes);
		}

		public void PutAddress(SystemAddress address)
		{
			if (address.GetVersion() != 4)
			{
				throw new Exception("Can't put IPv" + address.GetVersion() + ": not implemented");
			}
			PutByte((byte) address.GetVersion());
			foreach (var part in address.GetIpAddress().Split('.'))
			{
				PutByte((byte) ((byte) ~int.Parse(part) & 0xFF));
			}
			PutUnsignedShort((ushort) address.GetPort());
		}

		public void PutBoolean(bool b)
		{
			PutByte((byte) (b ? 1 : 0));
		}

		public void Skip(int len)
		{
			_buffer.Position += len;
		}

		public byte[] ToByteArray()
		{
			return _buffer.ToArray();
		}

		public int GetRemainingBytes()
		{
			return (int) (_buffer.Length - _buffer.Position);
		}

		private static bool IsLittleEndianMachine()
		{
			return BitConverter.IsLittleEndian;
		}

		public static JavaByteBuffer Allocate(int size, bool littleEndian)
		{
			var bb = new MemoryStream(size);
			//bb.order(order);
			return new JavaByteBuffer(bb);
		}

		public static JavaByteBuffer Wrap(byte[] bytes, bool littleEndian)
		{
			var bb = new MemoryStream(bytes);
			//bb.order(order);
			return new JavaByteBuffer(bb);
		}
	}
}