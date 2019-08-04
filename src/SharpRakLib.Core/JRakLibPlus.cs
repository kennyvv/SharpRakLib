using System;

namespace SharpRakLib
{
	public class JRakLibPlus
	{
		public const byte IdConnectedPingOpenConnections = 0x01;
		public const byte IdUnconnectedPingOpenConnections = 0x02;
		public const byte IdOpenConnectionRequest1 = 0x05;
		public const byte IdOpenConnectionReply1 = 0x06;
		public const byte IdOpenConnectionRequest2 = 0x07;
		public const byte IdOpenConnectionReply2 = 0x08;
		public const byte IdIncompatibleProtocolVersion = 0x1A;
		public const byte IdUnconnectedPongOpenConnections = 0x1C;
		public const byte IdAdvertiseSystem = 0x1D;

		public const byte CustomPacket0 = 0x80;
		public const byte CustomPacket1 = 0x81;
		public const byte CustomPacket2 = 0x82;
		public const byte CustomPacket3 = 0x83;
		public const byte CustomPacket4 = 0x84;
		public const byte CustomPacket5 = 0x85;
		public const byte CustomPacket6 = 0x86;
		public const byte CustomPacket7 = 0x87;
		public const byte CustomPacket8 = 0x88;
		public const byte CustomPacket9 = 0x89;
		public const byte CustomPacketA = 0x8A;
		public const byte CustomPacketB = 0x8B;
		public const byte CustomPacketC = 0x8C;
		public const byte CustomPacketD = 0x8D;
		public const byte CustomPacketE = 0x8E;
		public const byte CustomPacketF = 0x8F;

		public const byte Ack = 0xC0;
		public const byte Nack = 0xA0;
		//public const byte NACK = (byte) 0x04;

		public const byte McPing = 0x00;
		public const byte McPong = 0x03;

		public const byte McClientConnect = 0x09;
		public const byte McServerHandshake = 0x10;
		public const byte McClientHandshake = 0x13;
		public const byte McDisconnectNotification = 0x15;
		public static string LibraryVersion = "1.0-SNAPSHOT";

		public static int RaknetProtocol = 7;

		public static byte[] RaknetMagic =
		{
			0x00, 0xff, 0xff, 0x00,
			0xfe, 0xfe, 0xfe, 0xfe,
			0xfd, 0xfd, 0xfd, 0xfd,
			0x12, 0x34, 0x56, 0x78
		};


		public static byte[][] SplitByteArray(byte[] array, int chunkSize)
		{
			var splits = new byte[1024][];

			for (var i = 0; i < splits.Length; i++)
			{
				splits[i] = new byte[chunkSize];
			}

			var chunks = 0;
			for (var i = 0; i < array.Length; i += chunkSize)
			{
				if (array.Length - i > chunkSize)
				{
					Array.Copy(array, i, splits[chunks], 0, i + chunkSize);
				}
				else
				{
					Array.Copy(array, i, splits[chunks], 0, array.Length - i);
					//splits[chunks] = Arrays.copyOfRange(array, i, array.length);
				}
				chunks++;
			}

			//splits = Arrays.copyOf(splits, chunks);

			return splits;
		}
	}
}