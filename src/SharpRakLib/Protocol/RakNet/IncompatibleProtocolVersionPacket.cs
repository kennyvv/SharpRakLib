using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class IncompatibleProtocolVersionPacket : RakNetPacket
	{
		public byte ProtocolVersion;
		public long ServerId;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutByte(ProtocolVersion);
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutLong(ServerId);
		}

		public override void _decode(IBuffer buffer)
		{
			ProtocolVersion = buffer.GetByte();
			buffer.Skip(16); //MAGIC
			ServerId = buffer.GetLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdIncompatibleProtocolVersion;
		}

		public override int GetSize()
		{
			return 26;
		}
	}
}