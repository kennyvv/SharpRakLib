using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Util;

namespace SharpRakLib.Server
{
	public class RakNetServer : BaseSessionManager
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		
		private readonly Dictionary<string, long[]> _blacklist = new Dictionary<string, long[]>();
		private bool _disconnectInvalidProtocols;

		private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();
		private readonly UdpClient _socket;

		public RakNetServer(IPEndPoint bindAddress, ServerOptions options)
		{
			_socket = new UdpClient(bindAddress);
			this.BindAddress = bindAddress;

			BroadcastName = options.BroadcastName;
			MaxPacketsPerTick = options.MaxPacketsPerTick;
			ReceiveBufferSize = options.RecvBufferSize;
			SendBufferSize = options.SendBufferSize;
			PacketTimeout = options.PacketTimeout;
			PortChecking = options.PortChecking;
			_disconnectInvalidProtocols = options.DisconnectInvalidProtocol;
			ServerId = options.ServerId;
			WarnOnCantKeepUp = options.WarnOnCantKeepUp;

			_socket.Client.ReceiveBufferSize = ReceiveBufferSize;
			_socket.Client.SendBufferSize = SendBufferSize;
			
			AddTask(0, CheckBlacklist);
			AddShutdownTask(() =>
			{
				//socket.StopListeningAsync();
			});
		}

		public override void Start()
		{
			Running = true;
			Stopped = false;
			Run();
		}

		public override void Stop()
		{
			Running = false;
		}

		protected override int Send(byte[] data, int length, IPEndPoint endPoint)
		{
			return _socket.Send(data, length, endPoint);
		}

		protected override bool Bind()
		{
			try
			{
				_socket.EnableBroadcast = true;
				_socket.BeginReceive(RequestCallback, _socket);
			}
			catch (Exception e)
			{
				//this.logger.error("Failed to bind " + e.getClass().getSimpleName() + ": " + e.getMessage());
				Stop();
				return false;
			}
			return true;
		}

		private void RequestCallback(IAsyncResult ar)
		{
			var listener = (UdpClient) ar.AsyncState;
			if (listener.Client == null) return;

			var senderEndpoint = new IPEndPoint(0, 0);
			byte[] receiveBytes = null;
			try
			{
				receiveBytes = listener.EndReceive(ar, ref senderEndpoint);
			}
			catch (Exception e)
			{
				//Log.Error("Unexpected end of transmission?", e);
				if (listener.Client != null)
				{
					try
					{
						listener.BeginReceive(RequestCallback, listener);
					}
					catch (ObjectDisposedException dex)
					{
						//Log.Error("Unexpected end of transmission?", dex);
					}
				}

				return;
			}


			//	Log.Info("Received: " + receiveBytes.Length);
			if (receiveBytes.Length != 0)
			{
				listener.BeginReceive(RequestCallback, listener);				
				var packet = new DatagramPacket(receiveBytes, senderEndpoint.Address.ToString(), senderEndpoint.Port);
				HandlePacket(packet);
			}
		}

		private void CheckBlacklist()
		{
			lock (_blacklist)
			{
				if (_blacklist.Count != 0)
				{
					var toRemove = new List<string>();
					foreach (var i in _blacklist)
					{
						var millis = i.Value[0];
						var time = i.Value[1];
						if (time > 0)
						{
							if (ServerTime.ElapsedMilliseconds - millis >= time)
							{
								toRemove.Add(i.Key);
							}
						}
					}

					foreach (var i in toRemove)
					{
						_blacklist.Remove(i);
					}
				}
			}
			AddTask(0, CheckBlacklist);
		}

		private void HandlePacket(DatagramPacket packet)
		{
			var remote = packet.Endpoint;
			
			lock (_blacklist)
			{
				if (_blacklist.ContainsKey(remote.ToString())) return;
			}
			switch (packet.GetData()[0])
			{
				//Check for pings
				case JRakLibPlus.IdUnconnectedPingOpenConnections:
				{
					var upocp = new UnconnectedPingOpenConnectionsPacket();
					upocp.Decode(packet.GetData());

					var pong = new AdvertiseSystemPacket
					{
						ServerId = ServerId,
						PingId = upocp.PingId,
						Identifier = BroadcastName
					};
					AddPacketToQueue(pong, remote);
					break;
				}
				case JRakLibPlus.IdConnectedPingOpenConnections:
				{
					var ping = new ConnectedPingOpenConnectionsPacket();
					ping.Decode(packet.GetData());

					var pong2 = new UnconnectedPongOpenConnectionsPacket
					{
						ServerId = ServerId,
						PingId = ping.PingId,
						Identifier = BroadcastName
					};
					AddPacketToQueue(pong2, packet.Endpoint);
					break;
				}
				default:
					lock (_sessions)
					{
						Session session;
						if (!_sessions.ContainsKey("/" + remote))
						{
							session = new Session(remote, this);
							_sessions.Add("/" + session.Address, session);
							//	this.logger.debug("Session opened from " + packet.getAddress().toString());

							HookManager.ActivateHook(HookManager.Hook.SessionOpened, session);
						}
						else
						{
							session = _sessions["/" + remote];
						}

						session.ProcessPacket(packet.GetData());
					}
					break;
			}
		}

		internal void OnSessionClose(string reason, Session session)
		{
			//this.logger.debug("Session " + session.getAddress().toString() + " disconnected: " + reason);
			lock (_sessions)
			{
				_sessions.Remove("/" + session.Address);
			}

			HookManager.ActivateHook(HookManager.Hook.SessionClosed, session);
		}

		/**
		 * Blacklist an address for as long as the server is running. All packets
		 * from this address will be ignored.
		 * @param address The address to be blacklisted. Must include a port.
		 */

		public void AddToBlacklist(IPEndPoint address)
		{
			AddToBlacklist(address, -1);
		}

		/**
		 * Blacklsit an address for a certain amount of time. All packets
		 * for a certain amount of time will be ignored.
		 * @param address The address to be blacklisted. Must include a port
		 * @param time The amount of time for the address to be blacklisted. In milliseconds
		 */

		public void AddToBlacklist(IPEndPoint address, int time)
		{
			lock (_blacklist)
			{
				//this.logger.info("Added " + address.toString() + " to blacklist for " + time + "ms");
				_blacklist.Add(address.ToString(), new[] {ServerTime.ElapsedMilliseconds, time});
			}
		}

		internal void internal_addToBlacklist(IPEndPoint address, int time)
		{
			//Suppress log info
			lock (_blacklist)
			{
				_blacklist.Add(address.ToString(), new[] {ServerTime.ElapsedMilliseconds, time});
			}
		}

		/**
		 * Options the server uses to setup
		 */

		public class ServerOptions
		{
			public string BroadcastName = "A SharpRakLibPlus Server.";
			/**
			 * If this is true then the server will disconnect clients with invalid raknet protocols.
			 * The server currently supports protocol 7
			 */
			public bool DisconnectInvalidProtocol = true;
			/**
			 * The maximum amount of packets to read and process per tick (20 ticks per second)
			 */
			public int MaxPacketsPerTick = 500;
			public int PacketTimeout = 5000;
			public bool PortChecking = true;
			public int RecvBufferSize = 4096;
			public int SendBufferSize = 4096;
			/**
			 * The server's unique 64 bit identifier. This is usually generated
			 * randomly at start.
			 */
			public long ServerId = new Random().NextLong();
			/**
			 * If to log warning messages when a tick takes longer than 50 milliseconds.
			 */
			public bool WarnOnCantKeepUp = true;
		}
	}
}