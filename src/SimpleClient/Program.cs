using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;
using Jose;
using MiNET.Net;
using MiNET.Utils;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Core.Client;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using SimpleClient.Network;
using NewtonsoftMapper = MiNET.NewtonsoftMapper;
using Session = SharpRakLib.Server.Session;

namespace SimpleClient
{
    class Program
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        static void Main(string[] args)
        {
            ConfigureNLog(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            
            ThreadedRaknetClient client = new ThreadedRaknetClient(new IPEndPoint(IPAddress.Loopback, 19132));
            
            PacketHook hook;
            client.HookManager.AddHook(HookManager.Hook.PacketRecieved, hook = new PacketHook(client.Session));
            
            client.Start();
            
            Console.WriteLine("Started!");
            
            var session = client.WaitForSession();

            Console.WriteLine($"Session established!");
            Console.ReadLine();
            
            Console.WriteLine($"Finished!");
        }
        
        private static void ConfigureNLog(string baseDir)
        {
            string loggerConfigFile = Path.Combine(baseDir, "NLog.config");

            string logsDir = Path.Combine(baseDir, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(loggerConfigFile, true);
            LogManager.Configuration.Variables["basedir"] = baseDir;
        }
        
        private class PacketHook : HookManager.IHookRunnable
        {
            private McpeClientMessageDispatcher MessageDispatcher { get; }
            private SessionHandler SessionHandler { get; }
            private ClientSession ClientSession { get; }
            public PacketHook(ClientSession session)
            {
                ClientSession = session;
                SessionHandler = new SessionHandler(session);
                MessageDispatcher = new McpeClientMessageDispatcher(SessionHandler);
            }
            
            public void OnHook(SessionBase session, params object[] param)
            {
                EncapsulatedPacket pk = (EncapsulatedPacket) param[0];
                var id = pk.Payload[0];

                var packet = PacketFactory.Create(id, pk.Payload, "raknet");
                Log.Info($"Received: {packet}");
                HandleRakNet(packet);
                /* if (pk.Payload[0] == 0x10)
                 {
                     ServerHandshakePacket handshakePacket = new ServerHandshakePacket();
                     handshakePacket.Decode(pk.Payload);
                 
                     Log.Info($"Got handshake");
                 
                     ClientHandshakePacket handshake = new ClientHandshakePacket();
                     handshake.Address = new IPEndPoint(IPAddress.Loopback, 19132);
                     handshake.SendPing = handshakePacket.SendPing;
                     handshake.SystemAddresses = new IPEndPoint[20];
                     for (int i = 0; i < 20; i++)
                     {
                         handshake.SystemAddresses[i] = new IPEndPoint(IPAddress.Any, 0);
                     }
                 
                     EncapsulatedPacket encapsulatedPacket = new EncapsulatedPacket();
                     encapsulatedPacket.Payload = handshake.Encode();
                 
                     session.AddToQueue(encapsulatedPacket, true);
                 
                     return;
                 }*/
            }

            private void HandleRakNet(Packet message)
            {
                if (message == null)
                {
                    Log.Warn($"RakNet = null");
                    return;
                }

                if (message is ConnectionRequestAccepted requestAccepted)
                {
                    HandleConnectionRequestAccepted(requestAccepted);
                }
                else if (!MessageDispatcher.HandlePacket(message))
                {
                    Log.Warn(
                        $"Unhandled packet 0x{message.Id:X2} {message.GetType().Name}\n{Packet.HexDump(message.Bytes)}");
                }
            }

            private void SendPacket(Packet packet, bool immediate = false)
            {
                EncapsulatedPacket encapsulatedPacket = new EncapsulatedPacket();
                encapsulatedPacket.Payload = packet.Encode();
                
                ClientSession.AddToQueue(encapsulatedPacket, immediate);
            }
            
            private void HandleConnectionRequestAccepted(ConnectionRequestAccepted message)
            {
                var packet = NewIncomingConnection.CreateObject();
                packet.clientendpoint = new IPEndPoint(IPAddress.Loopback, 19132);
                packet.systemAddresses = new IPEndPoint[20];
                for (int i = 0; i < 20; i++)
                {
                    packet.systemAddresses[i] = new IPEndPoint(IPAddress.Any, 0);
                }
                
                SendPacket(packet, true);

                ClientSession._state = ClientSession.Connected;
                
                SendLogin("Test");

                /*ClientHandshakePacket handshake = new ClientHandshakePacket();
                handshake.Address = new IPEndPoint(IPAddress.Loopback, 19132);
                handshake.SendPing = message.serverTimestamp;
                handshake.SystemAddresses = new IPEndPoint[20];
                for (int i = 0; i < 20; i++)
                {
                    handshake.SystemAddresses[i] = new IPEndPoint(IPAddress.Any, 0);
                }
                
                EncapsulatedPacket encapsulatedPacket = new EncapsulatedPacket();
                encapsulatedPacket.Payload = handshake.Encode();
                
                ClientSession.AddToQueue(encapsulatedPacket, true);*/
            }
            
            public void SendLogin(string username)
            {
                JWT.JsonMapper = new NewtonsoftMapper();

                var clientKey = CryptoUtils.GenerateClientKey();
                byte[] data = CryptoUtils.CompressJwtBytes(CryptoUtils.EncodeJwt(username, clientKey, false), CryptoUtils.EncodeSkinJwt(clientKey, username), CompressionLevel.Fastest);

                McpeLogin loginPacket = new McpeLogin
                {
                    protocolVersion = Config.GetProperty("EnableEdu", false) ? 111 : McpeProtocolInfo.ProtocolVersion,
                    payload = data
                };

                /*Session.CryptoContext = new CryptoContext()
                {
                    ClientKey = clientKey,
                    UseEncryption = false,
                };*/

                SendPacket(loginPacket);
                Log.Info($"Login sent.");
            }
        }
    }
}