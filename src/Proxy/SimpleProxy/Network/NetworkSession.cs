using System;
using System.Net;
using System.Text;
using MiNET.Net;
using NLog;
using SharpRakLib;
using SharpRakLib.Core;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using DefaultMessageIdTypes = SharpRakLib.Core.DefaultMessageIdTypes;
using Reliability = SharpRakLib.Protocol.RakNet.Reliability;

namespace SimpleProxy.Network
{
	public class NetworkSession
	{
		private static ILogger Log = LogManager.GetCurrentClassLogger();
		private SessionBase Session { get; }
		private RakNetServer Server { get;}
		private int LastPing { get; set; } = -99;
	
		public NetworkSession(RakNetServer server, SessionBase baseSession)
		{
			Server = server;
			Session = baseSession;
		}

		public void HandlePacket(EncapsulatedPacket packet)
		{
			var pk = packet;
			
			Log.Info($"Got: {pk.ToString()} | {(DefaultMessageIdTypes) pk.Payload[0]} (0x{pk.Payload[0]:X2})");
			
			switch (pk.Payload[0])
			{
				case JRakLibPlus.McDisconnectNotification:
					Session.Disconnect("client disconnected");
					break;
				case JRakLibPlus.McClientConnect:
					var ccp = new ClientConnectPacket();
					ccp.Decode(pk.Payload);

					/*var shp = new ServerHandshakePacket();
					shp.Address = new IPEndPoint(IPAddress.Loopback, 19132);
					shp.SystemAddresses = new IPEndPoint[20];
					shp.SystemAddresses[0] = new IPEndPoint(IPAddress.Loopback, 19132);
					
					shp.SendPing = ccp.SendPing;
					shp.SendPong = DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

					for (int i = 1; i < 20; i++)
					{
						shp.SystemAddresses[i] = new IPEndPoint(IPAddress.Any, 19132);
					}*/
					
					
					var response = ConnectionRequestAccepted.CreateObject();
					response.NoBatch = true;
					response.systemAddress = new IPEndPoint(IPAddress.Loopback, 19132);
					response.systemAddresses = new IPEndPoint[20];
					response.systemAddresses[0] = new IPEndPoint(IPAddress.Loopback, 19132);
					response.incomingTimestamp = ccp.SendPing;
					response.serverTimestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

					for (int i = 1; i < 20; i++)
					{
						response.systemAddresses[i] = new IPEndPoint(IPAddress.Any, 19132);
					}
					
					var ep = new EncapsulatedPacket();
					ep.Reliability = Reliability.Unreliable;
					ep.Payload = response.Encode();
					Session.AddToQueue(ep, true);
					//Session.SendPacket(ep);
					return;

				case JRakLibPlus.McClientHandshake:
					/*if (_server.PortChecking)
					{
						var chp = new ClientHandshakePacket();
						chp.Decode(pk.Payload);
						if (chp.Address.Port != _server.BindAddress.Port)
						{
							Disconnect("Invalid Port");
						}
					}*/
					Session._state = SessionBase.Handshaking;

					PingClient();
					//this.server.addTask(3000, ()-> this.server.getLogger().debug("Client latency is " + getLastPing() + "ms"));
					break;

				case JRakLibPlus.McPing:
					var ping = new PingPacket();
					ping.Decode(pk.Payload);

					var pong = new PongPacket();
					pong.SendPingTime = ping.PingId;
					pong.SendPongTime = DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
					
					var ep2 = new EncapsulatedPacket();
					ep2.Reliability = Reliability.Unreliable;
					ep2.Payload = pong.Encode();
					Session.AddToQueue(ep2, true);
					break;

				case JRakLibPlus.McPong:
					var pong2 = new PongPacket();
					pong2.Decode(pk.Payload);

					LastPing = (int) (Server.Runtime - pong2.SendPingTime);
					break;
			}
			
			/*StringBuilder sb = new StringBuilder();
			sb.AppendLine($"{packet.GetType()} Payload:");
			foreach (var i in packet.Payload)
			{
				sb.Append(i.ToString("x2") + " ");
			}

			sb.AppendLine();
			sb.AppendLine();
			Log.Warn(sb.ToString());*/
			//Log.Warn("Received packet: " + packet);
		}
		
		public void PingClient()
		{
			var ping2 = new PingPacket();
			ping2.PingId = Server.Runtime;

			var ep3 = new EncapsulatedPacket();
			ep3.Reliability = Reliability.Unreliable;
			ep3.Payload = ping2.Encode();
			Session.AddToQueue(ep3, true);
		}

		internal void Destroy()
		{
			
		}
	}
}
