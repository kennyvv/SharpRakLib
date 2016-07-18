using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.Minecraft
{
	public class PingPacket : RakNetPacket
	{
		public long PingId;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutLong(PingId);
		}

		public override void _decode(IBuffer buffer)
		{
			PingId = buffer.GetLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McPing;
		}

		public override int GetSize()
		{
			return 9;
		}
	}
}