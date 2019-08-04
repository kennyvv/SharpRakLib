using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class DisconnectNotificationPacket : RakNetPacket
	{
		public override void _encode(BedrockStream buffer)
		{
		}

		public override void _decode(BedrockStream buffer)
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