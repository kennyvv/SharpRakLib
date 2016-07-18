using SharpRakLib.Nio;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionReply2Packet : RakNetPacket
	{
		public SystemAddress ClientAddress;
		public ushort MtuSize;
		public long ServerId;

		public override void _encode(IBuffer buffer)
		{
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutLong(ServerId);
			buffer.PutAddress(ClientAddress);
			buffer.PutUnsignedShort(MtuSize);
			buffer.PutByte(0); //security
		}

		public override void _decode(IBuffer buffer)
		{
			buffer.Skip(16); //MAGIC
			ServerId = buffer.GetLong();
			ClientAddress = buffer.GetAddress();
			MtuSize = buffer.GetUnsignedShort();
			//security
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionReply2;
		}

		public override int GetSize()
		{
			return 35;
		}
	}
}