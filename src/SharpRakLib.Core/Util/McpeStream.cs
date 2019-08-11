using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;

namespace SharpRakLib.Util
{
    public class BedrockStream : Stream
	{
		private CancellationTokenSource CancelationToken { get; }
		public Stream BaseStream { get; private set; }
		public BedrockStream(Stream baseStream)
		{
			BaseStream = baseStream;
			CancelationToken = new CancellationTokenSource();
		}

		public BedrockStream() : this(new MemoryStream())
		{
			
		}

		public override bool CanRead => BaseStream.CanRead;
		public override bool CanSeek => BaseStream.CanRead;
		public override bool CanWrite => BaseStream.CanRead;
		public override long Length => BaseStream.Length;

		public override long Position
		{
			get { return BaseStream.Position; }
			set { BaseStream.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return BaseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			BaseStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return BaseStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			BaseStream.Write(buffer, offset, count);
		}

		public override void Flush()
		{
			BaseStream.Flush();
		}

		#region Reader

		public override int ReadByte()
		{
			return BaseStream.ReadByte();
		}

		public byte[] Read(int length)
		{
			//byte[] d = new byte[length];
			//Read(d, 0, d.Length);
			//return d;

			SpinWait s = new SpinWait();
			int read = 0;

			var buffer = new byte[length];
			while (read < buffer.Length && !CancelationToken.IsCancellationRequested && s.Count < 25) //Give the network some time to catch up on sending data, but really 25 cycles should be enough.
			{
				int oldRead = read;

				int r = this.Read(buffer, read, length - read);
				if (r < 0) //No data read?
				{
					break;
				}

				read += r;

				if (read == oldRead)
				{
					s.SpinOnce();
				}
				if (CancelationToken.IsCancellationRequested) throw new ObjectDisposedException("");
			}

			return buffer;
		}


		public int ReadInt(bool bigEndian = false)
		{
			var dat = Read(4);
			var value = BitConverter.ToInt32(dat, 0);

			if (bigEndian)
				value = Endian.SwapInt32(value);
			
			return value;
		}

		public float ReadFloat()
		{
			var almost = Read(4);
			var f = BitConverter.ToSingle(almost, 0);
			return f;
		}

		public bool ReadBool()
		{
			var answer = ReadByte();
			if (answer == 1)
				return true;
			return false;
		}

		public double ReadDouble()
		{
			var almostValue = Read(8);
			return BitConverter.ToDouble(almostValue);
		}

		public int ReadVarInt()
		{
			int read = 0;
			return ReadVarInt(out read);
		}

		public int ReadVarInt(out int bytesRead)
		{
			int numRead = 0;
			int result = 0;
			byte read;
			do
			{
				read = (byte)ReadByte();
				int value = (read & 0x7f);
				result |= (value << (7 * numRead));

				numRead++;
				if (numRead > 5)
				{
					throw new Exception("VarInt is too big");
				}
			} while ((read & 0x80) != 0);
			bytesRead = numRead;
			return result;
		}

		public long ReadVarLong()
		{
			int numRead = 0;
			long result = 0;
			byte read;
			do
			{
				read = (byte)ReadByte();
				int value = (read & 0x7f);
				result |= (value << (7 * numRead));

				numRead++;
				if (numRead > 10)
				{
					throw new Exception("VarLong is too big");
				}
			} while ((read & 0x80) != 0);

			return result;
		}

		public short ReadShort()
		{
			var da = Read(2);
			var d = BitConverter.ToInt16(da, 0);
			return d;
		}

		public short ReadBEShort()
		{
			var da = Read(2);
			return Endian.SwapInt16(BitConverter.ToInt16(da, 0));
		}
		
		public ushort ReadUShort()
		{
			var da = Read(2);
			return BitConverter.ToUInt16(da, 0);
		}

		public ushort ReadBEUShort()
		{
			var da = Read(2);
			return Endian.SwapUInt16(BitConverter.ToUInt16(da, 0));
		}
		
		public ushort[] ReadUShort(int count)
		{
			var us = new ushort[count];
			for (var i = 0; i < us.Length; i++)
			{
				var da = Read(2);
				var d = BitConverter.ToUInt16(da, 0);
				us[i] = d;
			}
			return us;
		}

		public ushort[] ReadUShortLocal(int count)
		{
			var us = new ushort[count];
			for (var i = 0; i < us.Length; i++)
			{
				var da = Read(2);
				var d = BitConverter.ToUInt16(da, 0);
				us[i] = d;
			}
			return us;
		}

		public short[] ReadShortLocal(int count)
		{
			var us = new short[count];
			for (var i = 0; i < us.Length; i++)
			{
				var da = Read(2);
				var d = BitConverter.ToInt16(da, 0);
				us[i] = d;
			}
			return us;
		}

		public string ReadFixedString()
		{
			int length = Endian.SwapUInt16(ReadUShort());
			
			var stringValue = Read(length);
			return Encoding.UTF8.GetString(stringValue);
		}

		public string ReadString(bool varint = true)
		{
			var length = varint ? ReadVarInt() : ReadUShort();
			var stringValue = Read(length);

			return Encoding.UTF8.GetString(stringValue);
		}

		public long ReadLong()
		{
			var l = Read(8);
			return BitConverter.ToInt64(l, 0);
		}

		public ulong ReadULong()
		{
			var l = Read(8);
			return BitConverter.ToUInt64(l, 0);
		}

        public Vector3 ReadPosition()
		{
			var val = ReadLong();
			var x = Convert.ToSingle(val >> 38);
			var y = Convert.ToSingle(val & 0xFFF);
			var z = Convert.ToSingle((val << 38 >> 38) >> 12);

			/*if (x >= (2^25))
			{
				x -= 2^26;
			}
			if (y >= (2^11))
			{
				y -= 2^12;
			}
			if (z >= (2^25))
			{
				z -= 2^26;
			}*/

            return new Vector3(x, y, z);
		}

        public IPEndPoint ReadIpEndpoint()
        {
	        int version = ReadByte();
	        if (version == 4)
	        {
		        var address = (~ReadByte() & 0xff) + "." + (~ReadByte() & 0xff) + "." + (~ReadByte() & 0xff) + "." +
		                      (~ReadByte() & 0xff);
		        int port = ReadBEUShort();
		        return new IPEndPoint(IPAddress.Parse(address), port);
		      //  return new SystemAddress(address, port, version);
	        }
	        if (version == 6)
	        {
		        //TODO: IPv6 Decode
		        throw new Exception("Can't read IPv6 address: Not Implemented");
	        }
	        throw new Exception("Can't read IPv" + version + " address: unknown");
        }

        public int ReadLTriad()
        {
	        return (ReadByte() & 0xFF) | ((ReadByte() & 0xFF) << 8) | ((ReadByte() & 0x0F) << 16);
        }

        #endregion

        #region Writer

        public void Write(byte[] data)
		{
			this.Write(data, 0, data.Length);
		}

        public void WriteLTriad(int t)
        {
	        byte b1, b2, b3;
	        b3 = (byte) (t & 0xFF);
	        b2 = (byte) ((t >> 8) & 0xFF);
	        b1 = (byte) ((t >> 16) & 0xFF);
	        Write(new[] {b3, b2, b1});
        }

        public void Write(IPEndPoint endPoint)
        {
	        if (endPoint.AddressFamily == AddressFamily.InterNetwork)
	        {
		        WriteByte((byte) 4);
		        var parts = endPoint.Address.ToString().Split('.');
		        foreach (var part in parts)
		        {
			        WriteByte((byte) byte.Parse(part));
		        }
		        WriteBEShort((short) endPoint.Port);
	        }
        }
        
		public void WritePosition(Vector3 position)
		{
			var x = Convert.ToInt64(position.X);
			var y = Convert.ToInt64(position.Y);
			var z = Convert.ToInt64(position.Z);
			long toSend = ((x & 0x3FFFFFF) << 38) | ((z & 0x3FFFFFF) << 12) | (y & 0xFFF);
			WriteLong(toSend);
		}

	    /*public void WritePosition(BlockCoordinates pos)
	    {
            WritePosition(new Vector3(pos.X, pos.Y, pos.Z));
	    }*/

		public int WriteVarInt(int value)
		{
			int write = 0;
			do
			{
				byte temp = (byte)(value & 127);
				value >>= 7;
				if (value != 0)
				{
					temp |= 128;
				}
				WriteByte(temp);
				write++;
			} while (value != 0);
			return write;
		}

		public int WriteVarLong(long value)
		{
			int write = 0;
			do
			{
				byte temp = (byte)(value & 127);
				value >>= 7;
				if (value != 0)
				{
					temp |= 128;
				}
				WriteByte(temp);
				write++;
			} while (value != 0);
			return write;
		}

		public void WriteInt(int data)
		{
			var buffer = BitConverter.GetBytes(data);
			Write(buffer);
		}
		
		public void WriteBeInt(int data)
		{
			var buffer = BitConverter.GetBytes(Endian.SwapInt32(data));
			Write(buffer);
		}

		public void WriteFixedString(string data)
		{
			var stringData = Encoding.UTF8.GetBytes(data);
			WriteBEUShort((ushort) stringData.Length);
			
			Write(stringData);
		}

		public void WriteString(string data, bool varInt = true)
		{
			var stringData = Encoding.UTF8.GetBytes(data);
			if (varInt)
			{
				WriteVarInt(stringData.Length);
			}
			else
			{
				WriteUShort((ushort) stringData.Length);
			}

			Write(stringData);
		}

		public void WriteBEUShort(ushort data)
		{ 
			WriteUShort(Endian.SwapUInt16(data));
		}
		
		public void WriteBEShort(short data)
		{ 
			WriteShort(Endian.SwapInt16(data));
		}
		
		public void WriteShort(short data)
		{
			var shortData = BitConverter.GetBytes(data);
			Write(shortData);
		}

		public void WriteUShort(ushort data)
		{
			var uShortData = BitConverter.GetBytes(data);
			Write(uShortData);
		}

		public void WriteBool(bool data)
		{
			Write(BitConverter.GetBytes(data));
		}

		public void WriteDouble(double data)
		{
			Write(BitConverter.GetBytes(data));
		}

		public void WriteFloat(float data)
		{
			Write(BitConverter.GetBytes(data));
		}

		public void WriteLong(long data)
		{
			Write(BitConverter.GetBytes(data));
		}

		public void WriteULong(ulong data)
		{
			Write(BitConverter.GetBytes(data));
		}

        public void WriteUuid(Guid uuid)
		{
			var guid = uuid.ToByteArray();
			var long1 = new byte[8];
			var long2 = new byte[8];
			Array.Copy(guid, 0, long1, 0, 8);
			Array.Copy(guid, 8, long2, 0, 8);
			Write(long1);
			Write(long2);
		}

		public Guid ReadUuid()
		{
			var long1 = Read(8);
			var long2 = Read(8);

			return new Guid(long1.Concat(long2).ToArray());
		}

        #endregion

        private object _disposeLock = new object();
		private bool _disposed = false;
		protected override void Dispose(bool disposing)
		{
			if (!Monitor.IsEntered(_disposeLock))
				return;

			try
			{
				if (disposing && !_disposed)
				{
					_disposed = true;

					if (!CancelationToken.IsCancellationRequested)
						CancelationToken.Cancel();


				}
				base.Dispose(disposing);
			}
			finally
			{
				Monitor.Exit(_disposeLock);
			}
		}
	}
}