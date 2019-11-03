using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SharpRakLib.Core;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public abstract class CustomPacket : RakNetPacket
	{
		public List<EncapsulatedPacket> Packets = new List<EncapsulatedPacket>();
		public int SequenceNumber;

		private int CurrentSize { get; set; } = 0;
		private int FirstMessagId { get; set; } = 0;
		public bool TryAdd(EncapsulatedPacket packet, int mtuSize)
		{
			var bytes = packet.Encode();
			if (bytes.Length + CurrentSize > mtuSize)
			{
				return false;
			}

			if (packet.Split && Packets.Count > 0)
			{
				return false;
			}

			if (FirstMessagId == 0) FirstMessagId = packet.SplitId;
			
			Packets.Add(packet);
			CurrentSize += bytes.Length;

			return true;
		}

		public static IEnumerable<CustomPacket> CreateDatagrams(RakNetPacket packet, int mtuSize, SessionBase session)
		{
			return null;;
		}

		private static List<EncapsulatedPacket> GetParts(RakNetPacket packet, int mtuSize, Reliability reliability,
			SessionBase session)
		{
			var encoded = packet.Encode();

			int orderingIndex = 0;
			//reliability = Reliability.ReliableOrdered;
			
			if (reliability == Reliability.ReliableOrdered)
			{
			//	orderingIndex = Interlocked.Increment(ref session.OrderingIndex);
			}

			return null;
		}

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