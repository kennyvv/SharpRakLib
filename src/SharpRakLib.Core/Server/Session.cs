using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SharpRakLib.Core;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Util;

namespace SharpRakLib.Server
{
	public class Session
	{
		public const int Disconnected = 0;
		public const int Connecting1 = 1;
		public const int Connecting2 = 2;
		public const int Handshaking = 3;
		public const int Connected = 4;

		public const int MaxSplitSize = 128;
		public const int MaxSplitCount = 4;
		private readonly List<int> _ackQueue = new List<int>();
		private long _clientId;

		private int _lastPing = -99;

		private int _lastSeqNum = -1;

		private int _messageIndex;
		private short _mtu;
		private readonly List<int> _nackQueue = new List<int>();
		private readonly Dictionary<int, CustomPacket> _recoveryQueue = new Dictionary<int, CustomPacket>();

		private readonly CustomPacket _sendQueue = new CustomPackets.CustomPacket4();
		private int _sendSeqNum;

		private readonly RakNetServer _server;
		private int _splitId;

		private readonly Dictionary<int, Dictionary<int, EncapsulatedPacket>> _splitQueue =
			new Dictionary<int, Dictionary<int, EncapsulatedPacket>>();

		private int _state;
		private long _timeLastPacketReceived;

		public Session(IPEndPoint address, RakNetServer server)
		{
			this.Address = address;
			this._server = server;

			_state = Connecting1;

			this._server.AddTask(0, Update);
		}

		public IPEndPoint Address { get; }

		private void Update()
		{
			if (_state == Disconnected) return;
			if (JavaHelper.CurrentTimeMillis() - _timeLastPacketReceived >= _server.PacketTimeout)
			{
				Disconnect("timeout");
			}
			else
			{
				lock (_ackQueue)
				{
					if (_ackQueue.Count != 0)
					{
						var ack = new AckPacket();
						ack.Packets = _ackQueue.ToArray();
						SendPacket(ack);
						_ackQueue.Clear();
					}
				}
				lock (_nackQueue)
				{
					if (_nackQueue.Count != 0)
					{
						var nack = new NackPacket();
						nack.Packets = _nackQueue.ToArray();
						SendPacket(nack);
						_nackQueue.Clear();
					}
				}

				SendQueuedPackets();

				_server.AddTask(0, Update);
			}
		}

		private void SendQueuedPackets()
		{
			lock (_sendQueue)
			{
				if (_sendQueue.Packets.Count != 0)
				{
					_sendQueue.SequenceNumber = _sendSeqNum++;
					SendPacket(_sendQueue);
					lock (_recoveryQueue)
					{
						_recoveryQueue.Add(_sendQueue.SequenceNumber, _sendQueue);
					}

					_sendQueue.Packets.Clear();
				}
			}
		}

		public void SendPacket(RakNetPacket packet)
		{
			Console.WriteLine($"Sending: {packet.ToString()} | MTU: {_mtu}");
			
			_server.AddPacketToQueue(packet, Address);
		}

		public void AddPacketToQueue(EncapsulatedPacket pkt, bool immediate)
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
		}

		private void AddToQueue(EncapsulatedPacket pkt, bool immediate)
		{
			Console.WriteLine($"Queued: {pkt.ToString()} | {(DefaultMessageIdTypes) pkt.Payload[0]} | MTU: {_mtu}");
			if (immediate)
			{
				CustomPacket cp = new CustomPackets.CustomPacket0();
				cp.Packets.Add(pkt);
				cp.SequenceNumber = _sendSeqNum++;
				SendPacket(cp);
				lock (_recoveryQueue)
				{
					_recoveryQueue.Add(cp.SequenceNumber, cp);
				}
			}
			else
			{
				if (_sendQueue.GetSize() + pkt.GetSize() > _mtu)
				{
					SendQueuedPackets();
				}
				lock (_sendQueue)
				{
					_sendQueue.Packets.Add(pkt);
				}
			}
		}

		public void HandlePacket(byte[] data)
		{
			if (_state == Disconnected) return;
			_timeLastPacketReceived = JavaHelper.CurrentTimeMillis();
			
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
							Disconnect("Incorrect MTU");
							return;
						}

						var reply2 = new OpenConnectionReply2Packet();
						reply2.ServerId = _server.ServerId;
						reply2.MtuSize = (ushort) _mtu;
						reply2.ClientAddress = SystemAddress.FromIpEndPoint(Address);
						SendPacket(reply2);

						_state = Handshaking;
					}
					break;
				// ACK/NACK

				case JRakLibPlus.Ack:
					if (_state != Connected || _state == Handshaking) break;
					var ack = new AckPacket();
					ack.Decode(data);

					lock (_recoveryQueue)
					{
						foreach (var seq in ack.Packets)
						{
							if (_recoveryQueue.ContainsKey(seq))
							{
								_recoveryQueue.Remove(seq);
							}
						}
					}

					break;
				case JRakLibPlus.Nack:
					if (_state != Connected || _state == Handshaking) break;
					var nack = new NackPacket();
					nack.Decode(data);

					lock (_recoveryQueue)
					{
						foreach (var seq in nack.Packets)
						{
							if (_recoveryQueue.ContainsKey(seq))
							{
								var pk = _recoveryQueue[seq];
								pk.SequenceNumber = _sendSeqNum++;
								SendPacket(pk);
								_recoveryQueue.Remove(seq);
							}
						}
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

		private void HandleDataPacket(byte[] data)
		{
			CustomPacket pk = new CustomPackets.CustomPacket0();
			pk.Decode(data);

			var diff = pk.SequenceNumber - _lastSeqNum;
			lock (_nackQueue)
			{
				if (_nackQueue.Count != 0)
				{
					_nackQueue.Remove(pk.SequenceNumber);
					if (diff != 1)
					{
						for (var i = _lastSeqNum + 1; i < pk.SequenceNumber; i++)
						{
							_nackQueue.Add(i);
						}
					}
				}
			}
			lock (_ackQueue)
			{
				_ackQueue.Add(pk.SequenceNumber);
			}

			if (diff >= 1)
			{
				_lastSeqNum = pk.SequenceNumber;
			}

			pk.Packets.ForEach(HandleEncapsulatedPacket);
		}

		private void HandleSplitPacket(EncapsulatedPacket pk)
		{
			if (pk.SplitCount >= MaxSplitSize || pk.SplitIndex >= MaxSplitSize || pk.SplitIndex < 0)
			{
				return;
			}

			lock (_splitQueue)
			{
				if (!_splitQueue.ContainsKey(pk.SplitId))
				{
					if (_splitQueue.Count >= MaxSplitCount)
						return; //Too many split packets in the queue will increase memory usage
					var m = new Dictionary<int, EncapsulatedPacket>();
					m.Add(pk.SplitIndex, pk);
					_splitQueue.Add(pk.SplitId, m);
				}
				else
				{
					var m = _splitQueue[pk.SplitId];
					m.Add(pk.SplitIndex, pk);
					_splitQueue.Add(pk.SplitId, m);
				}

				if (_splitQueue[pk.SplitId].Count == pk.SplitCount)
				{
					var ep = new EncapsulatedPacket();
					
					var packets = _splitQueue[pk.SplitId];
					
					using (MemoryStream stream = new MemoryStream())
					{
						for (var i = 0; i < pk.SplitCount; i++)
						{
							stream.Write(packets[i].Payload);
						}

						ep.Payload = stream.ToArray();
					}
					_splitQueue.Remove(pk.SplitId);

					HandleEncapsulatedPacket(ep);
				}
			}
		}


		private void HandleEncapsulatedPacket(EncapsulatedPacket pk)
		{
			if (!(_state == Connected || _state == Handshaking)) return;
			if (pk.Split && _state == Connected)
			{
				HandleSplitPacket(pk);
			}

			switch (pk.Payload[0])
			{
				case JRakLibPlus.McDisconnectNotification:
					Disconnect("client disconnected");
					break;
				case JRakLibPlus.McClientConnect:
					var ccp = new ClientConnectPacket();
					ccp.Decode(pk.Payload);

					var shp = new ServerHandshakePacket();
					shp.Address = SystemAddress.FromIpEndPoint(Address);
					shp.SendPing = ccp.SendPing;
					shp.SendPong = ccp.SendPing + 1000L;

					var ep = new EncapsulatedPacket();
					ep.Reliability = Reliability.Unreliable;
					ep.Payload = shp.Encode();
					AddToQueue(ep, true);
					break;

				case JRakLibPlus.McClientHandshake:
					if (_server.PortChecking)
					{
						var chp = new ClientHandshakePacket();
						chp.Decode(pk.Payload);
						if (chp.Address.Port != _server.BindAddress.Port)
						{
							Disconnect("Invalid Port");
						}
					}
					_state = Connected;

					PingClient();
					//this.server.addTask(3000, ()-> this.server.getLogger().debug("Client latency is " + getLastPing() + "ms"));
					break;

				case JRakLibPlus.McPing:
					var ping = new PingPacket();
					ping.Decode(pk.Payload);

					var pong = new PongPacket();
					pong.SendPingTime = ping.PingId;
					pong.SendPongTime = DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
					
					var ep2 = new EncapsulatedPacket();
					ep2.Reliability = Reliability.Unreliable;
					ep2.Payload = pong.Encode();
					AddToQueue(ep2, true);
					break;

				case JRakLibPlus.McPong:
					var pong2 = new PongPacket();
					pong2.Decode(pk.Payload);

					_lastPing = (int) (JavaHelper.CurrentTimeMillis() - pong2.SendPingTime);
					break;

				default:
					_server.HookManager.ActivateHook(HookManager.Hook.PacketRecieved, this, pk);
					break;
			}
		}

		/**
		 * Pings the client. The latency in milliseconds is stored in the
		 * <code>lastPing</code> variable and can be retrieved using <code>getLastPing()</code>
		 */

		public void PingClient()
		{
			var ping2 = new PingPacket();
			ping2.PingId = JavaHelper.CurrentTimeMillis();

			var ep3 = new EncapsulatedPacket();
			ep3.Reliability = Reliability.Unreliable;
			ep3.Payload = ping2.Encode();
			AddToQueue(ep3, true);
		}

		public void Disconnect(string reason)
		{
			Console.WriteLine($"Sending disconnect: {reason}");
			
			var ep = new EncapsulatedPacket();
			ep.Reliability = Reliability.Unreliable;
			ep.Payload = new DisconnectNotificationPacket().Encode();
			AddToQueue(ep, true);

			_server.internal_addToBlacklist(Address, 500);
			// Prevent another session from opening, as the client will reply back with a few more packets

			_state = Disconnected;

			_server.OnSessionClose(reason, this);
		}
	}
}