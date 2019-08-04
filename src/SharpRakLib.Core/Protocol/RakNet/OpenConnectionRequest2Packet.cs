using System.IO;
using System.Net;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionRequest2Packet : RakNetPacket
	{
		public long ClientId;

		public short MtuSize;
		public IPEndPoint ServerAddress;

		public override void _encode(MinecraftStream buffer)
		{
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.Write(ServerAddress);
			buffer.WriteBEShort(MtuSize);
			buffer.WriteLong(ClientId);
		}

		public override void _decode(MinecraftStream buffer)
		{
			buffer.Seek(16, SeekOrigin.Current); //MAGIC
			ServerAddress = buffer.ReadIpEndpoint();
			MtuSize = buffer.ReadBEShort();
			ClientId = buffer.ReadUShort();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionRequest2;
		}

		public override int GetSize()
		{
			return 34;
		}
	}
}