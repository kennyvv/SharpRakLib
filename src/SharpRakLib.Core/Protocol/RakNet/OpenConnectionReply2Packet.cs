using System.IO;
using System.Net;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionReply2Packet : RakNetPacket
	{
		public IPEndPoint ClientAddress;
		public ushort MtuSize;
		public long ServerId;

		public override void _encode(BedrockStream buffer)
		{
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteLong(ServerId);
			buffer.Write(ClientAddress);
			buffer.WriteBEUShort(MtuSize);
			buffer.WriteByte(0); //security
		}

		public override void _decode(BedrockStream buffer)
		{
			buffer.Seek(16, SeekOrigin.Current); //MAGIC
			ServerId = buffer.ReadLong();
			ClientAddress = buffer.ReadIpEndpoint();
			MtuSize = buffer.ReadBEUShort();
			//security
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionReply2;
		}

		public override int GetSize()
		{
			return 35;
		}
	}
}