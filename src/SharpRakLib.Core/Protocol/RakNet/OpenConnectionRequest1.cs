using System.IO;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionRequest1Packet : RakNetPacket
	{
		public int NullPayloadLength;
		public byte ProtocolVersion;

		public override void _encode(MinecraftStream buffer)
		{
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteByte(ProtocolVersion);
			//buffer.Write(new byte[NullPayloadLength - 18]);
		}

		public override void _decode(MinecraftStream buffer)
		{
			buffer.Seek(16, SeekOrigin.Current); //MAGIC
			ProtocolVersion = (byte) buffer.ReadByte();
			NullPayloadLength = Raw.Length;
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionRequest1;
		}

		public override int GetSize()
		{
			return NullPayloadLength; //The payload length should be the entire length of the packet
		}
	}
}