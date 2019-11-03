using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Util;

namespace SharpRakLib.Server
{
	public class Session : SessionBase
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		
		private RakNetServer _server;
		public Session(IPEndPoint address, RakNetServer server) : base(address, server, 0)
		{
			_server = server;
		}

		/*public void AddPacketToQueue(EncapsulatedPacket pkt, bool immediate)
		{
			switch (pkt.Reliability)
			{
				case Reliability.Reliable:
				case Reliability.ReliableOrdered:
				//TODO: OrderIndex
				case Reliability.ReliableSequenced:
				case Reliability.ReliableWithAckReceipt:
				case Reliability.ReliableOrderedWithAckReceipt:
					pkt.MessageIndex = _messageIndex++;
					break;
			}

			if (pkt.GetSize() + 4 > _mtu)
			{
				// Too big to be sent in one packet, need to be split
				var buffers = JRakLibPlus.SplitByteArray(pkt.Payload, _mtu - 34);
				var splitId = this._splitId++%65536;
				for (var count = 0; count < buffers.Length; count++)
				{
					var ep = new EncapsulatedPacket();
					ep.SplitId = splitId;
					ep.Split = true;
					ep.SplitCount = buffers.Length;
					ep.Reliability = pkt.Reliability;
					ep.SplitIndex = count;
					ep.Payload = buffers[count];

					if (count > 0)
					{
						ep.MessageIndex = _messageIndex++;
					}
					else
					{
						ep.MessageIndex = pkt.MessageIndex;
					}
					if (ep.Reliability == Reliability.ReliableOrdered)
					{
						ep.OrderChannel = pkt.OrderChannel;
						ep.OrderIndex = pkt.OrderIndex;
					}

					AddToQueue(ep, true);
				}
			}
			else
			{
				AddToQueue(pkt, immediate);
			}
		}*/

		protected override void HandlePacket(byte[] data)
		{
			if (_state == Disconnected) return;

			_timeLastPacketReceived = SessionManager.Runtime;
			
			switch (data[0])
			{
				case JRakLibPlus.IdOpenConnectionRequest1:
					if (_state == Connecting1 || _state == Connecting2)
					{
						var req1 = new OpenConnectionRequest1Packet();
						req1.Decode(data);

						if (req1.ProtocolVersion != JRakLibPlus.RaknetProtocol)
						{
							var ipvp = new IncompatibleProtocolVersionPacket();
							ipvp.ProtocolVersion = (byte) JRakLibPlus.RaknetProtocol;
							ipvp.ServerId = _server.ServerId;
							_server.AddPacketToQueue(ipvp, Address);
						}
						_mtu = (short)req1.NullPayloadLength;

						Log.Info($"Got mtu: {req1.NullPayloadLength}");
						
						var reply1 = new OpenConnectionReply1Packet();
						reply1.MtuSize = _mtu;
						reply1.ServerId = _server.ServerId;
						SendPacket(reply1);

						_state = Connecting2;
					}
					break;
				case JRakLibPlus.IdOpenConnectionRequest2:
					if (_state == Connecting2)
					{
						var req2 = new OpenConnectionRequest2Packet();
						req2.Decode(data);

						_clientId = req2.ClientId;
						if (_server.PortChecking && req2.ServerAddress.Port != _server.BindAddress.Port)
						{
							Disconnect("Incorrect Port");
							return;
						}

						if (req2.MtuSize != _mtu)
						{
							Log.Info($"Incorrect MTU! Got: {req2.MtuSize} Expected: {_mtu}");
							Disconnect("Incorrect MTU");
							return;
						}

						var reply2 = new OpenConnectionReply2Packet();
						reply2.ServerId = _server.ServerId;
						reply2.MtuSize = (ushort) _mtu;
						reply2.ClientAddress = Address;
						SendPacket(reply2);

						_state = Handshaking;
					}
					break;
				default:
					if (_state == Connected || _state == Handshaking)
					{
						//noinspection ConstantConditions
						if (data[0] >= JRakLibPlus.CustomPacket0 && data[0] <= JRakLibPlus.CustomPacketF)
						{
							HandleDataPacket(data);
						}
					}
					///this.server.getLogger().debug("Unknown packet received: " + String.format("%02X", data[0]));
					break;
			}
		}


		protected override bool HandleEncapsulated(EncapsulatedPacket pk)
		{
			return false;
		}
		
		public override void Disconnect(string reason)
		{
			Console.WriteLine($"Sending disconnect: {reason}");
			
			var ep = new EncapsulatedPacket();
			ep.Reliability = Reliability.Unreliable;
			ep.Payload = new DisconnectNotificationPacket().Encode();
			AddPacketToQueue(ep, false);

			_server.internal_addToBlacklist(Address, 500);
			// Prevent another session from opening, as the client will reply back with a few more packets

			_state = Disconnected;

			_server.OnSessionClose(reason, this);
		}
	}
}