using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using NLog;
using NLog.Fluent;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.Minecraft;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using SharpRakLib.Util;

namespace SharpRakLib.Core.Client
{
    public class RakNetClient : BaseSessionManager
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        private UdpClient UdpClient { get; set; }

        private IPEndPoint ServerEndpoint { get; set; }
        public IPEndPoint ClientEndpoint { get; private set; }

        public ClientSession Session { get; set; }
        public RakNetClient(IPEndPoint endpoint)
        {
            Session = new ClientSession(this, endpoint, this, 1192);
            ClientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            ServerEndpoint = endpoint;
        }
        
        public override void Start()
        {
            if (Running) 
                return;
            
            Running = true;
            Stopped = false;

            Run();
        }

        /**
         * Stops the server. This method will not block, to check if
         * the server has finished it's last tick use <code>isStopped()</code>
         */

        public override void Stop()
        {
            Running = false;
            
            if (UdpClient == null) return; // Already stopped. It's ok.

            UdpClient.Close();
            UdpClient = null;
        }

        protected override bool Bind()
        {
            if (UdpClient != null) return false;

            try
            {
                UdpClient = new UdpClient(ClientEndpoint)
                {
                    Client =
                    {
                        ReceiveBufferSize = int.MaxValue,
                        SendBufferSize = int.MaxValue
                    },
                    DontFragment = false
                };

                if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
                {
                    // SIO_UDP_CONNRESET (opcode setting: I, T==3)
                    // Windows:  Controls whether UDP PORT_UNREACHABLE messages are reported.
                    // - Set to TRUE to enable reporting.
                    // - Set to FALSE to disable reporting.

                    uint IOC_IN = 0x80000000;
                    uint IOC_VENDOR = 0x18000000;
                    uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                    UdpClient.Client.IOControl((int) SIO_UDP_CONNRESET, new byte[] {Convert.ToByte(false)}, null);

                    ////
                    ////WARNING: We need to catch errors here to remove the code above.
                    ////
                }

                //Task.Run(ProcessQueue);

                ClientEndpoint = (IPEndPoint) UdpClient.Client.LocalEndPoint;
                
                UdpClient.BeginReceive(RequestCallback, UdpClient);
                
                return true;
            }
            catch (Exception e)
            {
              //  Log.Error("Main loop", e);
                Stop();
            }

            return false;
        }
        
        private void RequestCallback(IAsyncResult ar)
        {
            var listener = (UdpClient) ar.AsyncState;
            if (listener.Client == null) return;

            var senderEndpoint = new IPEndPoint(0, 0);
            byte[] receiveBytes = null;
            try
            {
                receiveBytes = listener.EndReceive(ar, ref senderEndpoint);
            }
            catch (Exception e)
            {
                //Log.Error("Unexpected end of transmission?", e);
                if (listener.Client != null)
                {
                    try
                    {
                        listener.BeginReceive(RequestCallback, listener);
                    }
                    catch (ObjectDisposedException dex)
                    {
                        //Log.Error("Unexpected end of transmission?", dex);
                    }
                }

                return;
            }


            //	Log.Info("Received: " + receiveBytes.Length);
            if (receiveBytes.Length != 0)
            {
                listener.BeginReceive(RequestCallback, listener);				
                var packet = new DatagramPacket(receiveBytes, senderEndpoint.Address.ToString(), senderEndpoint.Port);
                HandlePacket(packet);
            }
        }

        protected override int Send(byte[] data, int length, IPEndPoint endPoint)
        {
            return UdpClient.Send(data, length, endPoint);
        }

        internal void SendData(byte[] data, IPEndPoint target)
        {
            Send(data, data.Length, target);
        }

        public ClientSession WaitForSession()
        {
            while (!Session.IsConnected)
            {
                SpinWait.SpinUntil(() => UdpClient != null);
                
                RakNetClient.Log.Info($"No session...");
                //while (!client.IsConnected)
                {
                    ConnectedPingOpenConnectionsPacket packet = new ConnectedPingOpenConnectionsPacket();
                    packet.PingId = Stopwatch.GetTimestamp();
                    packet.Guid = new Random().Next();

                    var data = packet.Encode();
                    
                    if (ServerEndpoint != null)
                    {
                        SendData(data, ServerEndpoint);
                    }
                    else
                    {
                        SendData(data, new IPEndPoint(IPAddress.Broadcast, 19132));
                    }
                    
                    //Session.SendPacket(packet);
                
                    Thread.Sleep(500);
                }
            }

            RakNetClient.Log.Info($"Got session!");
            return Session;
        }
        
        private void HandlePacket(DatagramPacket packet)
        {
            Session.ProcessPacket(packet.GetData());
        }

        private bool UseSecurity { get; set; }
    }
}