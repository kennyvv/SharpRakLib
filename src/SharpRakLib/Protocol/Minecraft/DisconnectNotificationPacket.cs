using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.Minecraft
{
	public class DisconnectNotificationPacket : RakNetPacket
	{
		public override void _encode(IBuffer buffer)
		{
		}

		public override void _decode(IBuffer buffer)
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