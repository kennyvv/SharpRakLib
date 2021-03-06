﻿using System.Net;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
	public class ServerHandshakePacket : RakNetPacket
	{
		public IPEndPoint Address;

		public long SendPing;
		public long SendPong;

		public IPEndPoint[] SystemAddresses =
		{
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
			new IPEndPoint(IPAddress.Loopback, 0),
		};

		public override void _encode(BedrockStream buffer)
		{
			buffer.Write(Address);
			buffer.WriteBEShort(0);
			foreach (var a in SystemAddresses)
			{
				buffer.Write(a);
			}
			buffer.WriteLong(SendPing);
			buffer.WriteLong(SendPong);
		}

		public override void _decode(BedrockStream buffer)
		{
			buffer.ReadIpEndpoint();
			buffer.Position += 2; //short
			SystemAddresses = new IPEndPoint[10];
			for (var i = 0; i < 10; i++)
			{
				SystemAddresses[i] = buffer.ReadIpEndpoint();
			}
			SendPing = buffer.ReadLong();
			SendPong = buffer.ReadLong();
		}

		public override byte GetPid()
		{
			return JRakLibPlus.McServerHandshake;
		}

		public override int GetSize()
		{
			return 96;
		}
	}
}