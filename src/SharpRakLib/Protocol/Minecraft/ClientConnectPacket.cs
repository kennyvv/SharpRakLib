using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ClientConnectPacket : RakNetPacket
	{
		public long ClientId;
		public long SendPing;
		public bool UseSecurity;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutLong(ClientId);
			buffer.PutLong(SendPing);
			buffer.PutBoolean(UseSecurity);
		}

		public override void _decode(IBuffer buffer)
		{
			ClientId = buffer.GetLong();
			SendPing = buffer.GetLong();
			UseSecurity = buffer.GetBoolean();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McClientConnect;
		}

		public override int GetSize()
		{
			return 18;
		}
	}
}