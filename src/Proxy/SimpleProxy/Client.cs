using System.IO.Compression;
using System.Net;
using Jose;
using MiNET.Net;
using MiNET.Utils;
using NLog;
using SharpRakLib.Core;
using SharpRakLib.Core.Client;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using NewtonsoftMapper = MiNET.NewtonsoftMapper;

namespace SimpleProxy
{
    public class Client : ThreadedRaknetClient, HookManager.IHookRunnable
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        public Client(IPEndPoint endpoint) : base(endpoint)
        {
            HookManager.AddHook(HookManager.Hook.PacketRecieved, this);
        }
        
        public void OnHook(SessionBase session, params object[] param)
        {
            EncapsulatedPacket pk = (EncapsulatedPacket) param[0];
            var id = pk.Payload[0];

            var packet = PacketFactory.Create(id, pk.Payload, "raknet");
            Log.Info($"Received: {packet}");
            HandleRakNet(packet);
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
                else
                {
                    
                }
                /*else if (!MessageDispatcher.HandlePacket(message))
                {
                    Log.Warn(
                        $"Unhandled packet 0x{message.Id:X2} {message.GetType().Name}\n{Packet.HexDump(message.Bytes)}");
                }*/
            }

            private void SendPacket(Packet packet, bool immediate = false)
            {
                EncapsulatedPacket encapsulatedPacket = new EncapsulatedPacket();
                encapsulatedPacket.Payload = packet.Encode();
                
                Session.AddPacketToQueue(encapsulatedPacket, immediate);
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

                Session._state = ClientSession.Connected;
                
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