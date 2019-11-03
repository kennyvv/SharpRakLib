using System.Net;
using System.Threading;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using SimpleProxy.Network;
using SimpleProxy.Util;

namespace SimpleProxy
{
	public class SimpleServer
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		public SessionManager SessionManager { get; private set; }
		private RakNetServer Server { get; set; }
		private MotdProvider MotdProvider { get; set; }
		private Thread TickerThread { get; set; }
		public SimpleServer()
		{
			MotdProvider = new MotdProvider();
			
		}

		private long _currentTick = 0;
		private void Tick()
		{
			while (true)
			{
				try
				{
					long startTime = Server.Runtime;

					//Do Ticks
					if (_currentTick%5 == 0)
					{
						MotdProvider.OnlinePlayers = SessionManager.SessionCount;
						Server.BroadcastName = MotdProvider.GetMotd();
					}

					long endTime = Server.Runtime;

					if (endTime - startTime < 50)
					{
						Thread.Sleep((int) (50 - (endTime-startTime)));
					}
					else
					{
						//Overloaded?
					}

					_currentTick++;
				}
				catch (ThreadAbortException)
				{
					break;
				}
			}
		}

		public void Start()
		{
			if (Server != null) return; //Already started.

			Server = new ThreadedRaknetServer(new IPEndPoint(IPAddress.Any, Config.GetProperty("server-port", 19132)), new RakNetServer.ServerOptions()
			{
				BroadcastName = MotdProvider.GetMotd()
			});
			Server.HookManager.AddHook(HookManager.Hook.SessionOpened, new ConnectionOpenedHook(this));
			Server.HookManager.AddHook(HookManager.Hook.SessionClosed, new ConnectionClosedHook(this));
			Server.HookManager.AddHook(HookManager.Hook.PacketRecieved, new PacketHook(this));

			SessionManager = new SessionManager(Server);
			
			TickerThread = new Thread(Tick);

			TickerThread.Start();
			Server.Start();
			Log.Info("SimpleProxy is now running!");
		}

		public void Stop()
		{
			TickerThread.Abort();
			Server.Stop();
			Server = null;
		}

		protected void OnPacket(SessionBase session, EncapsulatedPacket packet)
		{
			NetworkSession netSession = SessionManager.GetSession(session);
			if (netSession != null)
			{
				netSession.HandlePacket(packet);
			}
		}

		private class ConnectionOpenedHook : HookManager.IHookRunnable
		{
			private SimpleServer Server { get; }
			public ConnectionOpenedHook(SimpleServer server)
			{
				Server = server;
			}

			public void OnHook(SessionBase session, params object[] param)
			{
				if (Server.SessionManager.SessionCount >= Server.MotdProvider.MaxPlayers)
				{
					//Do not create session.
					session.Disconnect("Server is full!");
					return;
				}
				Server.SessionManager.CreateSession(session);
			}
		}

		private class ConnectionClosedHook : HookManager.IHookRunnable
		{
			private SimpleServer Server { get; }
			public ConnectionClosedHook(SimpleServer server)
			{
				Server = server;
			}

			public void OnHook(SessionBase session, params object[] param)
			{
				Server.SessionManager.DestroySession(session);
			}
		}

		private class PacketHook : HookManager.IHookRunnable
		{
			private SimpleServer Server { get; }
			public PacketHook(SimpleServer server)
			{
				Server = server;
			}

			public void OnHook(SessionBase session, params object[] param)
			{
				Server.OnPacket(session, (EncapsulatedPacket) param[0]);
			}
		}
	}
}
