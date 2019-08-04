using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class IncompatibleProtocolVersionPacket : RakNetPacket
	{
		public byte ProtocolVersion;
		public long ServerId;

		public override void _encode(MinecraftStream buffer)
		{
			buffer.WriteByte(ProtocolVersion);
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteLong(ServerId);
		}

		public override void _decode(MinecraftStream buffer)
		{
			ProtocolVersion = (byte) buffer.ReadByte();
			buffer.Position += 16;//.Skip(16); //MAGIC
			ServerId = buffer.ReadLong();
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