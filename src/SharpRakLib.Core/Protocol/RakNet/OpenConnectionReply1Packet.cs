using System.IO;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class OpenConnectionReply1Packet : RakNetPacket
	{
		public short MtuSize;
		public long ServerId;
		public bool Security;
	
		public override void _encode(MinecraftStream buffer)
		{
			buffer.Write(JRakLibPlus.RaknetMagic);
			buffer.WriteLong(ServerId);
			buffer.WriteBool(Security); //Security
			buffer.WriteBEShort(MtuSize);
		}

		public override void _decode(MinecraftStream buffer)
		{
			//buffer.Seek(16, SeekOrigin.Current);//.Skip(16);
			buffer.Read(16);
			ServerId = buffer.ReadLong();
			Security = buffer.ReadBool(); //security
			MtuSize = buffer.ReadBEShort();
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