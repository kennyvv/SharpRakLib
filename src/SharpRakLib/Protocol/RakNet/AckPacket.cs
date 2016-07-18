namespace SharpRakLib.Protocol.RakNet
{
	public class AckPacket : AcknowledgePacket
	{
		public override byte GetPid()
		{
			return JRakLibPlus.Ack;
		}
	}
}