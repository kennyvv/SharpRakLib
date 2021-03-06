﻿using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class PongPacket : RakNetPacket
	{
		public long SendPingTime;
		public long SendPongTime;
		
		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLong(SendPingTime);
			buffer.WriteLong(SendPongTime);
		}

		public override void _decode(BedrockStream buffer)
		{
			SendPingTime = buffer.ReadLong();
			SendPongTime = buffer.ReadLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McPong;
		}

		public override int GetSize()
		{
			return 17;
		}
	}
}