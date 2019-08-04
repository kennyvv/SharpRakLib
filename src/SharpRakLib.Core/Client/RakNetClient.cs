using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Util;

namespace SharpRakLib.Core.Client
{
    public class RakNetClient
    {
        private readonly UdpClient _socket;
        private readonly Dictionary<TaskInfo, Action> _tasks = new Dictionary<TaskInfo, Action>();
        private readonly Queue<DatagramPacket> _sendQueue = new Queue<DatagramPacket>();

        public bool IsConnected { get; private set; } = false;
        private IPEndPoint ServerEndpoint { get; set; }
        public RakNetClient()
        {
            
            AddTask(0, HandlePackets);
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
                    _socket.Send(data, data.Length, pkt.Endpoint);
                }
                catch (IOException e)
                {
                    //this.logger.warn("java.io.IOException while sending packet: " + e.getMessage());
                }
            }
            AddTask(0, HandlePackets); //Run next tick
        }
        
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
                default:
                    
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