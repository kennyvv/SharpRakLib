using SharpRakLib.Nio;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ServerHandshakePacket : RakNetPacket
	{
		public SystemAddress Address;

		public long SendPing;
		public long SendPong;

		public SystemAddress[] SystemAddresses =
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

		public override void _encode(IBuffer buffer)
		{
			buffer.PutAddress(Address);
			buffer.PutShort(0);
			foreach (var a in SystemAddresses)
			{
				buffer.PutAddress(a);
			}
			buffer.PutLong(SendPing);
			buffer.PutLong(SendPong);
		}

		public override void _decode(IBuffer buffer)
		{
			buffer.GetAddress();
			buffer.Skip(2); //short
			SystemAddresses = new SystemAddress[10];
			for (var i = 0; i < 10; i++)
			{
				SystemAddresses[i] = buffer.GetAddress();
			}
			SendPing = buffer.GetLong();
			SendPong = buffer.GetLong();
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