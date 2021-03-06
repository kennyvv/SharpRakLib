﻿using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class PingPacket : RakNetPacket
	{
		public long PingId;

		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLong(PingId);
		}

		public override void _decode(BedrockStream buffer)
		{
			PingId = buffer.ReadLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McPing;
		}

		public override int GetSize()
		{
			return 9;
		}
	}
}