using System;
using System.Text;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SimpleServer.Network
{
	public class NetworkSession
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		private SessionBase Session { get; }
		public NetworkSession(SessionBase baseSession)
		{
			Session = baseSession;
		}

		public void HandlePacket(EncapsulatedPacket packet)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"{packet.GetType()} Payload:");
			foreach (var i in packet.Payload)
			{
				sb.Append(i.ToString("x2") + " ");
			}

			sb.AppendLine();
			sb.AppendLine();
			Log.Warn(sb.ToString());
			//Log.Warn("Received packet: " + packet);
		}

		internal void Destroy()
		{
			
		}
	}
}
