using System.Collections.Concurrent;
using System.Net;
using log4net;
using SharpRakLib.Server;
using SimpleServer.Network;

namespace SimpleServer
{
	public class SessionManager
	{
		private ILog Log = LogManager.GetLogger(typeof(SessionManager));
		private ConcurrentDictionary<IPEndPoint, NetworkSession> Sessions { get; }
		public SessionManager()
		{
			Sessions = new ConcurrentDictionary<IPEndPoint, NetworkSession>();
		}

		public int SessionCount
		{
			get { return Sessions.Count; }
		}

		public void CreateSession(Session session)
		{
			if (!Sessions.TryAdd(session.Address, new NetworkSession(session)))
			{
				Log.WarnFormat("Could not create session!");
			}
			else
			{
				Log.InfoFormat("Session started for {0}", session.Address);
			}
		}

		public void DestroySession(Session session)
		{
			NetworkSession outSession;
			if (!Sessions.TryRemove(session.Address, out outSession))
			{
				Log.Warn("Could not destroy session!");
			}
			outSession.Destroy();
		}

		public NetworkSession GetSession(Session session)
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
