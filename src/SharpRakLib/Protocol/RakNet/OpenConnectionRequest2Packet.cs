using SharpRakLib.Nio;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionRequest2Packet : RakNetPacket
	{
		public long ClientId;

		public ushort MtuSize;
		public SystemAddress ServerAddress;

		public override void _encode(IBuffer buffer)
		{
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutAddress(ServerAddress);
			buffer.PutUnsignedShort(MtuSize);
			buffer.PutLong(ClientId);
		}

		public override void _decode(IBuffer buffer)
		{
			buffer.Skip(16); //MAGIC
			ServerAddress = buffer.GetAddress();
			MtuSize = buffer.GetUnsignedShort();
			ClientId = buffer.GetUnsignedShort();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionRequest2;
		}

		public override int GetSize()
		{
			return 34;
		}
	}
}