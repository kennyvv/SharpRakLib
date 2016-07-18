using System;
using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public class EncapsulatedPacket : RakNetPacket
	{
		//If RELIABLE, RELIABLE_SEQUENCED, RELIABLE_ORDERED
		public int MessageIndex = -1;
		public int OrderChannel = -1;
		//If UNRELIABLE_SEQUENCED, RELIABLE_SEQUENCED, RELIABLE_ORDERED
		public int OrderIndex = -1;
		//Payload buffer
		public byte[] Payload = new byte[0];
		public Reliability Reliability;
		public bool Split;
		//If split
		public int SplitCount = -1;
		/**
		 * uint16 (unsigned short)
		 */
		public int SplitId = -1;
		public int SplitIndex = -1;

		public override void _encode(IBuffer buffer)
		{
			buffer.PutByte((byte) (((byte) Reliability << 5) | (Split ? 0x00010000 : 0)));

			buffer.PutUnsignedShort((ushort) (Payload.Length*8)); //Bytes to Bits

			if (Reliability == Reliability.Reliable || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				buffer.PutLTriad(MessageIndex);
			}

			if (Reliability == Reliability.UnreliableSequenced || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				buffer.PutLTriad(OrderIndex);
				buffer.PutByte((byte) OrderChannel);
			}

			if (Split)
			{
				buffer.PutInt(SplitCount);
				buffer.PutUnsignedShort((ushort) SplitId);
				buffer.PutInt(SplitIndex);
			}

			buffer.Put(Payload);
		}

		public override void _decode(IBuffer buffer)
		{
			//Header
			var flags = buffer.GetByte();
			Reliability = (Reliability) (byte) ((flags & 0x11100000) >> 5);
			Split = (flags & 0x00010000) > 0;
			var length = (int) Math.Ceiling(buffer.GetUnsignedShort()/8.0); //Bits to Bytes

			if (Reliability == Reliability.Reliable || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				MessageIndex = buffer.GetLTriad();
			}

			if (Reliability == Reliability.UnreliableSequenced || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				OrderIndex = buffer.GetLTriad();
				OrderChannel = buffer.GetByte();
			}

			if (Split)
			{
				SplitCount = buffer.GetInt();
				SplitId = buffer.GetUnsignedShort();
				SplitIndex = buffer.GetInt();
			}

			Payload = buffer.Get(length);
		}

		public override byte GetPid()
		{
			unchecked
			{
				return (byte) -1;
			}
		}

		public override int GetSize()
		{
			return 3 + Payload.Length + (MessageIndex != -1 ? 3 : 0) + (OrderIndex != -1 ? 4 : 0) + (Split ? 10 : 0);
		}
	}
}