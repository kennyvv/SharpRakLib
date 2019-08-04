using System;
using log4net;
using SharpRakLib.Core;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SimpleServer.Network
{
	public class NetworkSession
	{
		private ILog Log = LogManager.GetLogger(typeof(NetworkSession));
		private SessionBase Session { get; }
		public NetworkSession(SessionBase baseSession)
		{
			Session = baseSession;
		}

		public void HandlePacket(EncapsulatedPacket packet)
		{
			Log.Warn($"{packet.GetType()} Payload:");
			foreach (var i in packet.Payload)
			{
				Console.Write(i.ToString("x2") + " ");
			}
			Console.WriteLine();
			Console.WriteLine();
			//Log.Warn("Received packet: " + packet);
		}

		internal void Destroy()
		{
			
		}
	}
}
