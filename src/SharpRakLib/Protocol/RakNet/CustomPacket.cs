using System.Collections.Generic;
using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public abstract class CustomPacket : RakNetPacket
	{
		public List<EncapsulatedPacket> Packets = new List<EncapsulatedPacket>();
		public int SequenceNumber;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutLTriad(SequenceNumber);
			Packets.ForEach(packet => packet._encode(buffer));
		}

		public override void _decode(IBuffer buffer)
		{
			SequenceNumber = buffer.GetLTriad();
			while (buffer.GetRemainingBytes() >= 4)
			{
				//4 is the smallest amount of bytes an EncapsulatedPacket can be
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