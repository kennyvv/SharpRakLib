using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRakLib;
using SharpRakLib.Server;

namespace TestingRakLib
{
	class Program
	{
		private static ThreadedRaknetServer server { get; set; }
		static void Main(string[] args)
		{
			server = new ThreadedRaknetServer(new IPEndPoint(IPAddress.Any, 19132), new RakNetServer.ServerOptions());

			server.HookManager.AddHook(HookManager.Hook.SessionOpened, new SessionOpenedHook(server));
			server.HookManager.AddHook(HookManager.Hook.PacketRecieved, new PacketReceivedHook(server));
			server.HookManager.AddHook(HookManager.Hook.SessionClosed, new SessionClosedHook(server));

			server.Start();
			
			while (server.Running)
			{
				Thread.Sleep(50);
			}
		}
	}
}
