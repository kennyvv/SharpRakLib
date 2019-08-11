using System.Collections.Concurrent;
using System.Net;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Server;
using SimpleServer.Network;

namespace SimpleServer
{
	public class SessionManager
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		private ConcurrentDictionary<IPEndPoint, NetworkSession> Sessions { get; }
		public SessionManager()
		{
			Sessions = new ConcurrentDictionary<IPEndPoint, NetworkSession>();
		}

		public int SessionCount
		{
			get { return Sessions.Count; }
		}

		public void CreateSession(SessionBase session)
		{
			if (!Sessions.TryAdd(session.Address, new NetworkSession(session)))
			{
				Log.Warn("Could not create session!");
			}
			else
			{
				Log.Warn("Session started for {0}", session.Address);
			}
		}

		public void DestroySession(SessionBase session)
		{
			NetworkSession outSession;
			if (!Sessions.TryRemove(session.Address, out outSession))
			{
				Log.Warn("Could not destroy session!");
			}
			outSession.Destroy();
		}

		public NetworkSession GetSession(SessionBase session)
		{
			NetworkSession netSession;
			if (!Sessions.TryGetValue(session.Address, out netSession))
			{
				Log.Warn("Session not found!");
			}
			return netSession;
		}
	}
}
