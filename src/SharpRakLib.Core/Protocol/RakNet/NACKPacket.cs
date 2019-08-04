namespace SharpRakLib.Protocol.RakNet
{
	public class NackPacket : AcknowledgePacket
	{
		public override byte GetPid()
		{
			return JRakLibPlus.Nack;
		}
	}
}