using System;
using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class ConnectedPingOpenConnectionsPacket : RakNetPacket
	{
		public long PingId;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutLong(PingId);
			buffer.Put(JRakLibPlus.RaknetMagic);
		}

		public override void _decode(IBuffer buffer)
		{
			PingId = buffer.GetLong();
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