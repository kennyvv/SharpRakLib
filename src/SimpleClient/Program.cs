using System;
using System.Collections.Generic;
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
using Compression = SharpRakLib.Core.Compression;
using NewtonsoftMapper = MiNET.NewtonsoftMapper;
using Session = SharpRakLib.Server.Session;
using VarInt = MiNET.Utils.VarInt;

namespace SimpleClient
{
	public class Program
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        static void Main(string[] args)
        {
            ConfigureNLog(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            
            ThreadedRaknetClient client = new ThreadedRaknetClient(new IPEndPoint(IPAddress.Loopback, 19132));
            
            PacketHook hook;
            client.HookManager.AddHook(HookManager.Hook.PacketRecieved, hook = new PacketHook(client.Session, client));
            
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
        
        public class PacketHook : HookManager.IHookRunnable
        {
            private McpeClientMessageDispatcher MessageDispatcher { get; }
            private SessionHandler SessionHandler { get; }
            private ClientSession ClientSession { get; }
            private ISessionManager SessionManager { get; }
            
            public int PlayerStatus { get; set; } = 0;
            public PlayerLocation CurrentLocation { get; set; } = new PlayerLocation();
            public long EntityId { get; set; }
            public long NetworkEntityId { get; set; }
            
            public PacketHook(ClientSession session, ISessionManager sessionManager)
            {
	            SessionManager = sessionManager;
                ClientSession = session;
                SessionHandler = new SessionHandler(this, session);
                MessageDispatcher = new McpeClientMessageDispatcher(SessionHandler);
                
                SessionManager.AddTask(0, OnTick);
            }

            private void OnTick()
            {
	            if (PlayerStatus == 3)
	            {
		            SendMcpeMovePlayer();
	            }
	            
	            SessionManager.AddTask(50, OnTick);
            }

            public void SendMcpeMovePlayer()
            {
	            if (CurrentLocation == null) return;

	            McpeMovePlayer movePlayerPacket = McpeMovePlayer.CreateObject();
	            movePlayerPacket.runtimeEntityId = EntityId;
	            movePlayerPacket.x = CurrentLocation.X;
	            movePlayerPacket.y = CurrentLocation.Y;
	            movePlayerPacket.z = CurrentLocation.Z;
	            movePlayerPacket.yaw = CurrentLocation.Yaw;
	            movePlayerPacket.pitch = CurrentLocation.Pitch;
	            movePlayerPacket.headYaw = CurrentLocation.HeadYaw;

	            SendPacket(movePlayerPacket);
            }
            
            public void OnHook(SessionBase session, params object[] param)
            {
                EncapsulatedPacket pk = (EncapsulatedPacket) param[0];
                var id = pk.Payload[0];

                var packet = PacketFactory.Create(id, pk.Payload, "raknet");
                //Log.Info($"Received: {packet}");
                HandlePacket(packet);
            }

            private void HandlePacket(Packet message)
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
                else if (message is McpeWrapper wrapper)
                {
                    HandleMcpeWrapper(wrapper);
                }
                else if (!MessageDispatcher.HandlePacket(message))
                {
                    Log.Warn(
                        $"Unhandled packet 0x{message.Id:X2} {message.GetType().Name}\n{Packet.HexDump(message.Bytes)}");
                }
            }

            private void HandleMcpeWrapper(McpeWrapper batch)
            {
	            var messages = new List<Packet>();

	            // Get bytes
	            byte[] payload = batch.payload;
	            MemoryStream stream = new MemoryStream(payload);
	            if (stream.ReadByte() != 0x78)
	            {
		            throw new InvalidDataException("Incorrect ZLib header. Expected 0x78 0x9C");
	            }

	            stream.ReadByte();
	            using (var defStream2 = new DeflateStream(stream, CompressionMode.Decompress, false))
	            {
		            // Get actual package out of bytes
		            using (MemoryStream destination = new MemoryStream())
		            {
			            defStream2.CopyTo(destination);
			            destination.Position = 0;
			            do
			            {
				            byte[] internalBuffer = null;
				            try
				            {
					            int len = (int) VarInt.ReadUInt32(destination);
					            long pos = destination.Position;
					            int id = (int) VarInt.ReadUInt32(destination);
					            len = (int) (len - (destination.Position -
					                                pos)); // calculate len of buffer after varint
					            internalBuffer = new byte[len];
					            destination.Read(internalBuffer, 0, len);

					            if (id == 0x8e)
						            throw new Exception("Wrong code, didn't expect a 0x8E in a batched packet");

					            var packet = PacketFactory.Create((byte) id, internalBuffer, "mcpe") ??
					                         new UnknownPacket((byte) id, internalBuffer);
					            messages.Add(packet);

					            //if (Log.IsDebugEnabled) Log.Debug($"Batch: {packet.GetType().Name} 0x{packet.Id:x2}");
					            if (packet is UnknownPacket)
						            Log.Error($"Batch: {packet.GetType().Name} 0x{packet.Id:x2}");
					            //if (!(package is McpeFullChunkData)) Log.Debug($"Batch: {package.GetType().Name} 0x{package.Id:x2} \n{Package.HexDump(internalBuffer)}");
				            }
				            catch (Exception e)
				            {
					            if (internalBuffer != null)
						            Log.Error($"Batch error while reading:\n{Packet.HexDump(internalBuffer)}");
					            Log.Error("Batch processing", e);
				            }
			            } while (destination.Position < destination.Length);
		            }
	            }

	            //Log.Error($"Batch had {messages.Count} packets.");
	            if (messages.Count == 0) Log.Error($"Batch had 0 packets.");

	            foreach (var msg in messages)
	            {
		            msg.DatagramSequenceNumber = batch.DatagramSequenceNumber;
		            msg.OrderingChannel = batch.OrderingChannel;
		            msg.OrderingIndex = batch.OrderingIndex;
		            HandlePacket(msg);
		            msg.PutPool();
	            }
            }

            public void SendPacket(Packet packet, bool immediate = true)
            {
	            if (ClientSession._state == ClientSession.Connected && !(packet is McpeWrapper))
               {
                   McpeWrapper wrapperPacket = new McpeWrapper();
                   wrapperPacket.payload = Compression.Compress(packet.Encode(), true);
                   packet = wrapperPacket;
               }
               
                EncapsulatedPacket encapsulatedPacket = new EncapsulatedPacket();
                encapsulatedPacket.Payload = packet.Encode();
                
                ClientSession.AddPacketToQueue(encapsulatedPacket, immediate);
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
                
                ClientSession._state = ClientSession.Connected;
                SendPacket(packet, true);

                SendLogin("Test");
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

                Thread.Sleep(50);

                SendPacket(loginPacket, true);
                Log.Info($"Login sent.");
            }
        }
    }
}