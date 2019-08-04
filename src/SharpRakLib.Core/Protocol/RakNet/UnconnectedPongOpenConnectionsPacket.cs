using System.IO;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class UnconnectedPongOpenConnectionsPacket : RakNetPacket
	{
		public string Identifier;
		public long PingId;
		public long ServerId;

		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLong(PingId);
			buffer.WriteLong(ServerId);
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteFixedString(Identifier);
		}

		public override void _decode(BedrockStream buffer)
		{
			PingId = buffer.ReadLong();
			ServerId = buffer.ReadLong();
			buffer.Seek(16, SeekOrigin.Current); //MAGIC
			Identifier = buffer.ReadFixedString();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdUnconnectedPongOpenConnections;
		}

		public override int GetSize()
		{
			return 35 + Identifier.GetBytes().Length;
		}
	}
}