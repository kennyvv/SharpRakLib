using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class DisconnectNotificationPacket : RakNetPacket
	{
		public override void _encode(MinecraftStream buffer)
		{
		}

		public override void _decode(MinecraftStream buffer)
		{
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McDisconnectNotification;
		}

		public override int GetSize()
		{
			return 1;
		}
	}
}