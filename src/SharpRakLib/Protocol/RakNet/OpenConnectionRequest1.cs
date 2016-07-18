using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionRequest1Packet : RakNetPacket
	{
		public int NullPayloadLength;
		public byte ProtocolVersion;

		public override void _encode(IBuffer buffer)
		{
			buffer.Put(JRakLibPlus.RaknetMagic);
			buffer.PutByte(ProtocolVersion);
			buffer.Put(new byte[NullPayloadLength - 18]);
		}

		public override void _decode(IBuffer buffer)
		{
			buffer.Skip(16); //MAGIC
			ProtocolVersion = buffer.GetByte();
			NullPayloadLength = buffer.GetRemainingBytes() + 18;
		}

		public override byte GetPid()
		{
			return JRakLibPlus.IdOpenConnectionRequest1;
		}

		public override int GetSize()
		{
			return NullPayloadLength; //The payload length should be the entire length of the packet
		}
	}
}