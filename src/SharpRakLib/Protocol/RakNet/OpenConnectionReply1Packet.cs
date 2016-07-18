using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionReply1Packet : RakNetPacket
	{
		public ushort MtuSize;
		public long ServerId;

		public override void _encode(IBuffer buffer)
		{
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutLong(ServerId);
			buffer.PutByte(0); //Security
			buffer.PutUnsignedShort(MtuSize);
		}

		public override void _decode(IBuffer buffer)
		{
			buffer.Skip(16);
			ServerId = buffer.GetLong();
			buffer.GetByte(); //security
			MtuSize = buffer.GetUnsignedShort();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionReply1;
		}

		public override int GetSize()
		{
			return 28;
		}
	}
}