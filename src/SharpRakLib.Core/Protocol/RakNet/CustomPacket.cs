using System;
using System.Collections.Generic;
using System.IO;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public abstract class CustomPacket : RakNetPacket
	{
		public List<EncapsulatedPacket> Packets = new List<EncapsulatedPacket>();
		public int SequenceNumber;

		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteLTriad(SequenceNumber);
			Packets.ForEach(packet => packet._encode(buffer));
		}

		public override void _decode(BedrockStream buffer)
		{
			SequenceNumber = buffer.ReadLTriad();
			while (buffer.Position < buffer.Length)
			{
				var packet = new EncapsulatedPacket();
				packet._decode(buffer);
				Packets.Add(packet);
			}
		}

		public override int GetSize()
		{
			var len = 4;
			foreach (var packet in Packets)
			{
				len = len + packet.GetSize();
			}
			return len;
		}
	}
}