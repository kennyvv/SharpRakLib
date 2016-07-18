namespace SharpRakLib.Protocol.RakNet
{
	public class UnconnectedPingOpenConnectionsPacket : ConnectedPingOpenConnectionsPacket
	{
		public override byte GetPid()
		{
			return JRakLibPlus.IdUnconnectedPingOpenConnections;
		}
	}
}