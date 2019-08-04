namespace SharpRakLib.Protocol.RakNet
{
	public class AdvertiseSystemPacket : UnconnectedPongOpenConnectionsPacket
	{
		public override byte GetPid()
		{
			return JRakLibPlus.IdAdvertiseSystem;
		}
	}
}