using System.Net;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ServerHandshakePacket : RakNetPacket
	{
		public IPEndPoint Address;

		public long SendPing;
		public long SendPong;

		public IPEndPoint[] SystemAddresses =
		{
			new SystemAddress("127.0.0.1", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4),
			new SystemAddress("0.0.0.0", 0, 4)
		};

		public override void _encode(MinecraftStream buffer)
		{
			buffer.Write(Address);
			buffer.WriteShort(0);
			foreach (var a in SystemAddresses)
			{
				buffer.Write(a);
			}
			buffer.WriteLong(SendPing);
			buffer.WriteLong(SendPong);
		}

		public override void _decode(MinecraftStream buffer)
		{
			buffer.ReadIpEndpoint();
			buffer.Position += 2; //short
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
			return JRakLibPlus.McServerHandshake;
		}

		public override int GetSize()
		{
			return 96;
		}
	}
}