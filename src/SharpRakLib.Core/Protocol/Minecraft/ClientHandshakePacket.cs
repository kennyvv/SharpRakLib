using System.Net;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ClientHandshakePacket : RakNetPacket
	{
		public IPEndPoint Address;
		public long SendPing;
		public long SendPong;
		public IPEndPoint[] SystemAddresses;

		public override void _encode(BedrockStream buffer)
		{
			buffer.Write(Address);
			foreach (var a in SystemAddresses)
			{
				buffer.Write(a);
			}
			buffer.WriteLong(SendPing);
			buffer.WriteLong(SendPong);
		}

		public override void _decode(BedrockStream buffer)
		{
			Address = buffer.ReadIpEndpoint();
			SystemAddresses = new IPEndPoint[10];
			for (var i = 0; i < 10; i++)
			{
				SystemAddresses[i] = buffer.ReadIpEndpoint();
			}
			SendPing = buffer.ReadLong();
			SendPong = buffer.ReadLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McClientHandshake;
		}

		public override int GetSize()
		{
			return 94;
		}
	}
}