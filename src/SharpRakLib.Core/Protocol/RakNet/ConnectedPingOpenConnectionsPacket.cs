using System;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class ConnectedPingOpenConnectionsPacket : RakNetPacket
	{
		public long PingId;

		public override void _encode(MinecraftStream buffer)
		{
			buffer.WriteLong(PingId);
			buffer.Write(JRakLibPlus.RaknetMagic);
		}

		public override void _decode(MinecraftStream buffer)
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