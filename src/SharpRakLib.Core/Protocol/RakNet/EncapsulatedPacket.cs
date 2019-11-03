using System;
using NLog;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public class EncapsulatedPacket : RakNetPacket
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		
		//If RELIABLE, RELIABLE_SEQUENCED, RELIABLE_ORDERED
		public int MessageIndex = -1;
		public int OrderChannel = -1;
		//If UNRELIABLE_SEQUENCED, RELIABLE_SEQUENCED, RELIABLE_ORDERED
		public int OrderIndex = -1;
		//Payload buffer
		public byte[] Payload = new byte[0];
		public Reliability Reliability = Reliability.ReliableOrdered;
		public bool Split;
		//If split
		public int SplitCount = -1;
		/**
		 * uint16 (unsigned short)
		 */
		public int SplitId = -1;
		public int SplitIndex = -1;

		public override void _encode(BedrockStream buffer)
		{
			buffer.WriteByte((byte) ((((byte)Reliability) << 5) | (Split ? Convert.ToByte("00010000", 2) : 0x00)));
			buffer.WriteBEShort((short) (Payload.Length*8)); //Bytes to Bits

			if (Reliability == Reliability.Reliable || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				buffer.WriteLTriad(MessageIndex);
			}

			if (Reliability == Reliability.UnreliableSequenced || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				buffer.WriteLTriad(OrderIndex);
				buffer.WriteByte((byte) OrderChannel);
			}

			if (Split)
			{
				buffer.WriteBeInt(SplitCount);
				buffer.WriteBEShort((short) SplitId);
				buffer.WriteBeInt(SplitIndex);
			}

			buffer.Write(Payload);
		}

		public override void _decode(BedrockStream buffer)
		{
			//Header
			var flags = buffer.ReadByte();
			Reliability = (Reliability) ((flags & Convert.ToByte("011100000", 2)) >> 5);;
			Split = ((flags & Convert.ToByte("00010000", 2)) > 0);// (flags & 0x00010000) > 0;
			
			var dataBitLength = buffer.ReadBEShort();

			if (Reliability == Reliability.Reliable || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				MessageIndex = buffer.ReadLTriad();
			}

			if (Reliability == Reliability.UnreliableSequenced || Reliability == Reliability.ReliableSequenced ||
			    Reliability == Reliability.ReliableOrdered)
			{
				OrderIndex = buffer.ReadLTriad();
				OrderChannel = buffer.ReadByte();
			}

			if (Split)
			{
				SplitCount = buffer.ReadInt(true); //Endian.SwapInt32(buffer.ReadInt());
				SplitId = buffer.ReadBEShort();//  Endian.SwapUInt16(buffer.ReadUShort());
				SplitIndex = buffer.ReadInt(true);
			}

			var msgLength = (int) Math.Ceiling(((double) dataBitLength) / 8);
			
			Payload = buffer.Read(msgLength);
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