using System;
using SharpRakLib.Server;

namespace TestingRakLib
{
	public class SessionOpenedHook :  HookManager.IHookRunnable
	{
		private RakNetServer Server { get; }
		public SessionOpenedHook(RakNetServer server)
		{
			Server = server;
		}

		public void OnHook(Session session, params object[] param)
		{
			Console.WriteLine("Session opened: " + session.Address);
		} 
	}

	public class SessionClosedHook : HookManager.IHookRunnable
	{
		private RakNetServer Server { get; }
		public SessionClosedHook(RakNetServer server)
		{
			Server = server;
		}

		public void OnHook(Session session, params object[] param)
		{
			Console.WriteLine("Session closed: " + session.Address);
		}
	}
}
