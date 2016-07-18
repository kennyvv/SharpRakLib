using SharpRakLib.Nio;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ClientHandshakePacket : RakNetPacket
	{
		public SystemAddress Address;
		public long SendPing;
		public long SendPong;
		public SystemAddress[] SystemAddresses;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutAddress(Address);
			foreach (var a in SystemAddresses)
			{
				buffer.PutAddress(a);
			}
			buffer.PutLong(SendPing);
			buffer.PutLong(SendPong);
		}

		public override void _decode(IBuffer buffer)
		{
			Address = buffer.GetAddress();
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
			return JRakLibPlus.McClientHandshake;
		}

		public override int GetSize()
		{
			return 94;
		}
	}
}