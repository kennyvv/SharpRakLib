using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SharpRakLib.Core;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Util;

namespace SharpRakLib.Server
{
	public class RakNetServer : ISessionManager
	{
		private readonly Dictionary<string, long[]> _blacklist = new Dictionary<string, long[]>();
		private bool _disconnectInvalidProtocols;
		//private UdpSocketReceiver socket;
		private readonly Queue<DatagramPacket> _sendQueue = new Queue<DatagramPacket>();

		private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();

		private readonly List<Action> _shutdownTasks = new List<Action>();

		private readonly UdpClient _socket;
		private readonly Dictionary<TaskInfo, Action> _tasks = new Dictionary<TaskInfo, Action>();

		public RakNetServer(IPEndPoint bindAddress, ServerOptions options)
		{
			_socket = new UdpClient(bindAddress);
			//socket = new UdpSocketReceiver();	
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

			//this.logger = LoggerFactory.getLogger("JRakLibPlus Server");

			HookManager = new HookManager(this);

			_socket.Client.ReceiveBufferSize = ReceiveBufferSize;
			_socket.Client.SendBufferSize = SendBufferSize;

			AddTask(0, HandlePackets);
			AddTask(0, CheckBlacklist);
			AddShutdownTask(() =>
			{
				//socket.StopListeningAsync();
			});
		}

		public bool Running { get; set; }
		public bool Stopped { get; set; } = true;
		//private Logger logger;
		public string BroadcastName { get; set; }
		public int MaxPacketsPerTick { get; set; }
		public int ReceiveBufferSize { get; }
		public int SendBufferSize { get; }
		public int PacketTimeout { get; set; }
		public bool PortChecking { get; set; }
		public long ServerId { get; }
		public bool WarnOnCantKeepUp { get; }

		public IPEndPoint BindAddress { get; }
		public HookManager HookManager { get; }

		/**
     * Starts the server in the current thread. This method will block
     * as long as the server is running.
     */

		public void Start()
		{
			Running = true;
			Stopped = false;
			Run();
		}

		/**
		 * Stops the server. This method will not block, to check if
		 * the server has finished it's last tick use <code>isStopped()</code>
		 */

		public void Stop()
		{
			Running = false;
		}

		protected virtual void Run()
		{
			//this.logger.info("Server starting...");
			Console.WriteLine("Starting server...");
			if (Bind())
			{
				Console.WriteLine("RakNetServer bound to " + BindAddress + ", running on RakNet protocol " +
				                  JRakLibPlus.RaknetProtocol);
				//this.logger.info("RakNetServer bound to " + bindAddress + ", running on RakNet protocol " + JRakLibPlus.RAKNET_PROTOCOL);
				try
				{
					while (Running)
					{
						var start = JavaHelper.CurrentTimeMillis();
						Tick();
						var elapsed = JavaHelper.CurrentTimeMillis() - start;
						if (elapsed >= 50)
						{
							if (WarnOnCantKeepUp)
								Console.WriteLine("Can't keep up, did the system time change or is the server overloaded? (" + elapsed + ">50)");
							//this.logger.warn("Can't keep up, did the system time change or is the server overloaded? (" + elapsed + ">50)");
						}
						else
						{
							Thread.Sleep((int) (50 - elapsed));
						}
					}
				}
				catch (Exception e)
				{
					//this.logger.error("Fatal Exception, server has crashed! " + e.Source + ": " + e);
					//e.printStackTrace();
					Stop();
				}
			}

			//this.shutdownTasks.ForEach(Runnable::run);
			lock (_shutdownTasks)
			{
				foreach (var i in _shutdownTasks)
				{
					i.Invoke();
				}
			}

			Stopped = true;
			//	this.logger.info("Server has stopped.");
		}

		private void Tick()
		{
			if (this._tasks.Count == 0) return;
			lock (this._tasks)
			{
				var remove = new List<TaskInfo>();
				var tasks = new Dictionary<TaskInfo, Action>(this._tasks);
				foreach (var ti in tasks.Keys.Where(ti => JavaHelper.CurrentTimeMillis() - ti.RegisteredAt >= ti.RunIn))
				{
					try
					{
						this._tasks[ti].Invoke();
					}
					catch (Exception ex)
					{
						Console.WriteLine("Exception: " + ex);
					}
					remove.Add(ti);
				}
				foreach (var i in remove)
				{
					tasks.Remove(i);
				}
			}
		}

		private bool Bind()
		{
			try
			{
				//	this.socket.StartListeningAsync(bindAddress.Port);
				//this.socket.MessageReceived += MessageReceived;

				//this.socket.setBroadcast(true);
				//this.socket.setSendBufferSize(this.sendBufferSize);
				//this.socket.setReceiveBufferSize(this.receiveBufferSize);

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


			//	Console.WriteLine("Received: " + receiveBytes.Length);
			if (receiveBytes.Length != 0)
			{
				listener.BeginReceive(RequestCallback, listener);				
				var packet = new DatagramPacket(receiveBytes, senderEndpoint.Address.ToString(), senderEndpoint.Port);
				HandlePacket(packet);
			}
		}

		public void AddTask(long runIn, Action r)
		{
			lock (_tasks)
			{
				var ti = new TaskInfo
				{
					RunIn = runIn,
					RegisteredAt = JavaHelper.CurrentTimeMillis()
				};
				if (!_tasks.ContainsKey(ti))
				{
					_tasks.Add(ti, r);
				}
			}
		}

		private void HandlePackets()
		{
			while (_sendQueue.Count != 0)
			{
				var pkt = _sendQueue.Dequeue();
				try
				{
					var data = pkt.GetData();
					_socket.Send(data, data.Length, pkt.Endpoint);
				}
				catch (IOException e)
				{
					//this.logger.warn("java.io.IOException while sending packet: " + e.getMessage());
				}
			}
			AddTask(0, HandlePackets); //Run next tick
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
							if (JavaHelper.CurrentTimeMillis() - millis >= time)
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

		public void AddPacketToQueue(RakNetPacket packet, IPEndPoint address)
		{
			lock (_sendQueue)
			{
				var buffer = packet.Encode();
				_sendQueue.Enqueue(new DatagramPacket(buffer, address.Address.ToString(), address.Port));
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
				_blacklist.Add(address.ToString(), new[] {JavaHelper.CurrentTimeMillis(), time});
			}
		}

		internal void internal_addToBlacklist(IPEndPoint address, int time)
		{
			//Suppress log info
			lock (_blacklist)
			{
				_blacklist.Add(address.ToString(), new[] {JavaHelper.CurrentTimeMillis(), time});
			}
		}

		/**
		 * Adds a task to be ran when the server shuts down.
		 * @param r The task to be ran.
		 */

		public void AddShutdownTask(Action r)
		{
			lock (_shutdownTasks)
			{
				_shutdownTasks.Add(r);
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