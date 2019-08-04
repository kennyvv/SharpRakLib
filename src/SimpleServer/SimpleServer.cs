using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SharpRakLib;
using SharpRakLib.Core;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using SimpleServer.Network;
using SimpleServer.Util;

namespace SimpleServer
{
	public class SimpleServer
	{
		private ILog Log = LogManager.GetLogger(typeof(SimpleServer));
		public SessionManager SessionManager { get; }
		private RakNetServer Server { get; set; }
		private MotdProvider MotdProvider { get; set; }
		private Thread TickerThread { get; set; }
		public SimpleServer()
		{
			SessionManager = new SessionManager();
			MotdProvider = new MotdProvider();
			
		}

		private long _currentTick = 0;
		private void Tick()
		{
			while (true)
			{
				try
				{
					long startTime = JavaHelper.CurrentTimeMillis();

					//Do Ticks
					if (_currentTick%5 == 0)
					{
						MotdProvider.OnlinePlayers = SessionManager.SessionCount;
						Server.BroadcastName = MotdProvider.GetMotd();
					}

					long endTime = JavaHelper.CurrentTimeMillis();

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

			TickerThread = new Thread(Tick);

			TickerThread.Start();
			Server.Start();
			Log.Info("SimpleServer is now running!");
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
