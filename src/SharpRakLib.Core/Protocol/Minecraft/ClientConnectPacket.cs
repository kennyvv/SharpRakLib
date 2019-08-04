using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ClientConnectPacket : RakNetPacket
	{
		public long ClientId;
		public long SendPing;
		public bool UseSecurity;

		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLong(ClientId);
			buffer.WriteLong(SendPing);
			buffer.WriteBool(UseSecurity);
		}

		public override void _decode(BedrockStream buffer)
		{
			ClientId = buffer.ReadLong();
			SendPing = buffer.ReadLong();
			UseSecurity = buffer.ReadBool();
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