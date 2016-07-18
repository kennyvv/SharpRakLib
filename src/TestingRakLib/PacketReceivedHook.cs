using System;
using SharpRakLib.Server;

namespace TestingRakLib
{
	public class PacketReceivedHook : HookManager.IHookRunnable
	{
		private RakNetServer Server { get; }
		public PacketReceivedHook(RakNetServer server)
		{
			Server = server;
		}

		public void OnHook(Session session, params object[] param)
		{
			Console.WriteLine("Received packet: " + param[0]);
		}
	}
}
