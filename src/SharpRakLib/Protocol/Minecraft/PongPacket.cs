namespace SharpRakLib.Protocol.Minecraft
{
	public class PongPacket : PingPacket
	{
		public override byte GetPid()
		{
			return JRakLibPlus.McPong;
		}
	}
}