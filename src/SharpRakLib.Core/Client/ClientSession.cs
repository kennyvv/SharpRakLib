using System;
using System.Net;
using System.Threading;
using NLog;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SharpRakLib.Core.Client
{
    public class ClientSession : SessionBase
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        private RakNetClient Client { get; }
        public ClientSession(RakNetClient client, IPEndPoint address, ISessionManager manager, short mtu) : base(address, manager, mtu)
        {
            Client = client;
        }

        protected override void HandlePacket(byte[] data)
        {
            var id = data[0];

            if (id <= (byte) 126)
            {
                switch (id)
                {
                    //Check for pings
                    case JRakLibPlus.IdUnconnectedPongOpenConnections:
                        UnconnectedPongOpenConnectionsPacket p = new UnconnectedPongOpenConnectionsPacket();
                        p.Decode(data);

                        OnUnconnectedPong(p, Address);
                        break;
                    case JRakLibPlus.IdOpenConnectionReply1:
                        OpenConnectionReply1Packet reply = new OpenConnectionReply1Packet();
                        reply.Decode(data);

                        _mtu = reply.MtuSize;
                        
                        SendOpenConnectionRequest2(reply, Address);
                        break;
                    case JRakLibPlus.IdOpenConnectionReply2:
                        var pack = new OpenConnectionReply2Packet();
                        pack.Decode(data);
                        OnOpenConnectionReply2(pack, Address);
                        break;
                    default:
                        Log.Warn($"RakNet packet unhandled: {id} | 0x{id:X2}");
                        // Session?.ProcessPacket(packet.GetData());
                        break;
                }
            }
            else
            {
                if (_state == Connected || _state == Handshaking)
                {
                    //noinspection ConstantConditions
                    if (id >= JRakLibPlus.CustomPacket0 && id <= JRakLibPlus.CustomPacketF)
                    {
                        HandleDataPacket(data);
                    }
                }
                else
                {
                    Log.Warn($"Got packet in unexpected state: {id} | 0x{id:X2}");
                }
            }

        }
        
        protected override bool HandleEncapsulated(EncapsulatedPacket pk)
        {
          //  Log.Info($"Got encapsulated package: {pk} | 0x{pk.Payload[0]:X2}");
            return false;
        }

        private void OnOpenConnectionReply2(OpenConnectionReply2Packet packet, IPEndPoint senderEndPoint)
        {
            _mtu = (short) packet.MtuSize;
            
            //Thread.Sleep(250);
            
            ClientConnectPacket connectPacket = new ClientConnectPacket();
            connectPacket.ClientId = _clientGuid;
            connectPacket.SendPing = DateTime.UtcNow.Ticks;
            connectPacket.UseSecurity = false;
            
            var ep = new EncapsulatedPacket();
            ep.Reliability = Reliability.Unreliable;
            ep.Payload = connectPacket.Encode();
            AddPacketToQueue(ep, true);

            _state = Handshaking;
        }

        public bool IsConnected { get; set; } = false;
        protected virtual void OnUnconnectedPong(UnconnectedPongOpenConnectionsPacket packet, IPEndPoint senderEndpoint)
        {
            if (!IsConnected)
            {
                IsConnected = true;
                SendOpenConnectionRequest1(senderEndpoint);
            }
        }

        /* public void SendConnectedPing()
         {
             var packet = new ConnectedPing()
             {
                 sendpingtime = DateTime.UtcNow.Ticks
             };
 
             SendPacket(packet);
         }
 
         public void SendConnectedPong(long sendpingtime)
         {
             var packet = new ConnectedPong
             {
                 sendpingtime = sendpingtime,
                 sendpongtime = sendpingtime + 200
             };
 
             SendPacket(packet);
         }
 */
        public void SendOpenConnectionRequest1(IPEndPoint target)
        {
            var packet = new OpenConnectionRequest1Packet()
            {
                ProtocolVersion = (byte) JRakLibPlus.RaknetProtocol
            };
            
            //SendPacket(packet);
            
            //AddPacketToQueue(packet, ServerEndpoint);

            byte[] data = packet.Encode();

 
            // 1446 - 1464
            // 1087 1447
            byte[] data2 = new byte[_mtu - data.Length - 10];
            Buffer.BlockCopy(data, 0, data2, 0, data.Length);

            Client.SendData(data2, target);
            // Client.SendData(data2, target);
        }

        private long _clientGuid;
        public void SendOpenConnectionRequest2(OpenConnectionReply1Packet reply, IPEndPoint remote)
        {
           // UseSecurity = reply.Security;
            //_mtu = reply.MtuSize;
  
            var packet = new OpenConnectionRequest2Packet()
            {
                ClientId = _clientGuid,
                MtuSize = (short) _mtu,
                ServerAddress = Address,
                SendTime = reply.SendTime
            };
            
            SendPacket(packet);

           // var data = packet.Encode();
            //SendData(data, remote);

            //AddPacketToQueue(packet, ServerEndpoint);
        }
    }
}