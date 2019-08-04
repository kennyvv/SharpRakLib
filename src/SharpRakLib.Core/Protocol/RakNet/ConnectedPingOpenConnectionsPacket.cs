using System;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class ConnectedPingOpenConnectionsPacket : RakNetPacket
	{
		public long PingId;
		public long Guid;
		
		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLong(PingId);
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteLong(Guid);
		}

		public override void _decode(BedrockStream buffer)
		{
			PingId = buffer.ReadLong();
			//MAGIC
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdConnectedPingOpenConnections;
		}

		public override int GetSize()
		{
			return 25;
		}
	}
}