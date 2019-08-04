using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;
using SharpRakLib.Util;

namespace SharpRakLib.Core.Client
{
    public class RakNetClient : ISessionManager
    {
        private UdpClient UdpClient { get; set; }
        private readonly Dictionary<TaskInfo, Action> _tasks = new Dictionary<TaskInfo, Action>();
        private readonly Queue<DatagramPacket> _sendQueue = new Queue<DatagramPacket>();

        public bool Running { get; set; }
        public bool Stopped { get; set; }
        public string BroadcastName { get; set; }
        public int MaxPacketsPerTick { get; set; }
        public int ReceiveBufferSize { get; }
        public int SendBufferSize { get; }
        public int PacketTimeout { get; set; }
        public bool PortChecking { get; set; }
        public long ServerId { get; }
        public bool WarnOnCantKeepUp { get; }
        public IPEndPoint BindAddress { get; }
        public HookManager HookManager { get; }
        
        public bool IsConnected { get; private set; } = false;
        private IPEndPoint ServerEndpoint { get; set; }
        public IPEndPoint ClientEndpoint { get; private set; }
        
        public SessionBase Session { get; private set; }
        public RakNetClient(IPEndPoint endpoint)
        {
            ClientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            ServerEndpoint = endpoint;
            
            HookManager = new HookManager(this);

            //UdpClient.Client.ReceiveBufferSize = ReceiveBufferSize;
           // UdpClient.Client.SendBufferSize = SendBufferSize;

            AddTask(0, HandlePackets);
            //AddTask(0, CheckBlacklist);
            Session = new SessionBase(endpoint, this);
        }
        
        public void Start()
        {
            Running = true;
            Stopped = false;
            Run();
        }

        /**
         * Stops the server. This method will not block, to check if
         * the server has finished it's last tick use <code>isStopped()</code>
         */

        public void Stop()
        {
            Running = false;
            
            if (UdpClient == null) return; // Already stopped. It's ok.

            UdpClient.Close();
            UdpClient = null;
        }

        private bool Bind()
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


            //	Console.WriteLine("Received: " + receiveBytes.Length);
            if (receiveBytes.Length != 0)
            {
                listener.BeginReceive(RequestCallback, listener);				
                var packet = new DatagramPacket(receiveBytes, senderEndpoint.Address.ToString(), senderEndpoint.Port);
                HandlePacket(packet);
            }
        }
        
        protected virtual void Run()
        {
            //this.logger.info("Server starting...");
            Console.WriteLine("Starting client...");
            if (Bind())
            {
                Console.WriteLine("RakNetClient bound to " + ClientEndpoint + ", running on RakNet protocol " +
                                  JRakLibPlus.RaknetProtocol);
                //this.logger.info("RakNetServer bound to " + bindAddress + ", running on RakNet protocol " + JRakLibPlus.RAKNET_PROTOCOL);
                try
                {
                    while (Running)
                    {
                        var start = JavaHelper.CurrentTimeMillis();
                        Tick();
                        var elapsed = JavaHelper.CurrentTimeMillis() - start;
                        if (elapsed >= 50)
                        {
                            if (WarnOnCantKeepUp)
                                Console.WriteLine("Can't keep up, did the system time change or is the server overloaded? (" + elapsed + ">50)");
                            //this.logger.warn("Can't keep up, did the system time change or is the server overloaded? (" + elapsed + ">50)");
                        }
                        else
                        {
                            Thread.Sleep((int) (50 - elapsed));
                        }
                    }
                }
                catch (Exception e)
                {
                    //this.logger.error("Fatal Exception, server has crashed! " + e.Source + ": " + e);
                    //e.printStackTrace();
                    Stop();
                }
            }

            Stopped = true;
            //	this.logger.info("Server has stopped.");
        }

        private void Tick()
        {
            if (this._tasks.Count == 0) return;
            lock (this._tasks)
            {
                var remove = new List<TaskInfo>();
                var tasks = new Dictionary<TaskInfo, Action>(this._tasks);
                foreach (var ti in tasks.Keys.Where(ti => JavaHelper.CurrentTimeMillis() - ti.RegisteredAt >= ti.RunIn))
                {
                    try
                    {
                        this._tasks[ti].Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: " + ex);
                    }
                    remove.Add(ti);
                }
                foreach (var i in remove)
                {
                    tasks.Remove(i);
                }
            }
        }

        public void AddTask(long runIn, Action r)
        {
            lock (_tasks)
            {
                var ti = new TaskInfo
                {
                    RunIn = runIn,
                    RegisteredAt = JavaHelper.CurrentTimeMillis()
                };
                if (!_tasks.ContainsKey(ti))
                {
                    _tasks.Add(ti, r);
                }
            }
        }

        private void HandlePackets()
        {
            while (_sendQueue.Count != 0)
            {
                var pkt = _sendQueue.Dequeue();
                try
                {
                    var data = pkt.GetData();
                    UdpClient.Send(data, data.Length, pkt.Endpoint);
                }
                catch (IOException e)
                {
                    //this.logger.warn("java.io.IOException while sending packet: " + e.getMessage());
                }
            }
            AddTask(0, HandlePackets); //Run next tick
        }
        
        private readonly Dictionary<string, SessionBase> _sessions = new Dictionary<string, SessionBase>();
        private void HandlePacket(DatagramPacket packet)
        {
            var remote = packet.Endpoint;
            
            switch (packet.GetData()[0])
            {
                //Check for pings
                case JRakLibPlus.IdUnconnectedPongOpenConnections:
                    UnconnectedPongOpenConnectionsPacket p = new UnconnectedPongOpenConnectionsPacket();
                    p.Decode(packet.GetData());
                    
                    OnUnconnectedPong(p, remote);
                    break;
                case JRakLibPlus.IdOpenConnectionReply1:
                    break;
                case JRakLibPlus.IdOpenConnectionReply2:
                    break;
                default:
                    lock (_sessions)
                    {
                        SessionBase session;
                        if (!_sessions.ContainsKey("/" + remote))
                        {
                            session = new SessionBase(remote, this);
                            _sessions.Add("/" + session.Address, session);
                            //	this.logger.debug("Session opened from " + packet.getAddress().toString());

                            HookManager.ActivateHook(HookManager.Hook.SessionOpened, session);
                        }
                        else
                        {
                            session = _sessions["/" + remote];
                        }

                        session.ProcessPacket(packet.GetData());
                    }
                    break;
            }
        }
        
        protected virtual void OnUnconnectedPong(UnconnectedPongOpenConnectionsPacket packet, IPEndPoint senderEndpoint)
        {
            if (!IsConnected)
            {
                ServerEndpoint = senderEndpoint;
                IsConnected = true;
              //  SendOpenConnectionRequest1();
            }
        }
        
        private void OnOpenConnectionReply2(OpenConnectionReply2Packet message)
        {
           // Log.Warn("MTU Size: " + message.mtuSize);
         //   Log.Warn("Client Endpoint: " + message.clientEndpoint);

            //_serverEndpoint = message.clientEndpoint;

           // _mtuSize = message.mtuSize;
          //  SendConnectionRequest();
        }

        public void AddPacketToQueue(RakNetPacket packet, IPEndPoint address)
        {
            lock (_sendQueue)
            {
                var buffer = packet.Encode();
                _sendQueue.Enqueue(new DatagramPacket(buffer, address.Address.ToString(), address.Port));
            }
        }
    }
}