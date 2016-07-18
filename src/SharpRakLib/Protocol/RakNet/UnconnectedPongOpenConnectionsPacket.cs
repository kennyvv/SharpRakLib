using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class UnconnectedPongOpenConnectionsPacket : RakNetPacket
	{
		public string Identifier;
		public long PingId;
		public long ServerId;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutLong(PingId);
			buffer.PutLong(ServerId);
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutString(Identifier);
		}

		public override void _decode(IBuffer buffer)
		{
			PingId = buffer.GetLong();
			ServerId = buffer.GetLong();
			buffer.Skip(16); //MAGIC
			Identifier = buffer.GetString();
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