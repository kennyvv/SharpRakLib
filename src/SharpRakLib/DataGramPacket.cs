using System.Net;

namespace SharpRakLib
{
	public class DatagramPacket
	{
		public DatagramPacket(byte[] data, string remoteHost, int remotePort)
		{
			Data = data;
			RemoteAddress = remoteHost;
			RemotePort = remotePort;
		}

		private byte[] Data { get; }
		private string RemoteAddress { get; }
		private int RemotePort { get; }

		public byte[] GetData()
		{
			return Data;
		}

		public IPEndPoint GetSocketAddress()
		{
			return new IPEndPoint(IPAddress.Parse(RemoteAddress), RemotePort);
		}

		public string GetAddress()
		{
			return RemoteAddress;
		}

		public int GetPort()
		{
			return RemotePort;
		}
	}
}