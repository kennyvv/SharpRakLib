using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;
using NLog.Fluent;
using SharpRakLib.Protocol;
using SharpRakLib.Server;
using SharpRakLib.Util;

namespace SharpRakLib.Core
{
    public abstract class BaseSessionManager : ISessionManager
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        private readonly List<Action> _shutdownTasks = new List<Action>();
        private readonly Dictionary<TaskInfo, Action> _tasks = new Dictionary<TaskInfo, Action>();
        private readonly Queue<DatagramPacket> _sendQueue = new Queue<DatagramPacket>();

        protected Stopwatch ServerTime { get; }
        public long Runtime => ServerTime.ElapsedMilliseconds;
        
        protected BaseSessionManager()
        {
            ServerTime = Stopwatch.StartNew();
            HookManager = new HookManager(this);
            
            AddTask(0, HandlePackets);
        }
        
        public bool Running { get; set; }
        public bool Stopped { get; set; }
        public string BroadcastName { get; set; }
        public int MaxPacketsPerTick { get; set; }
        public int ReceiveBufferSize { get; protected set; }
        public int SendBufferSize { get; protected set;}
        public int PacketTimeout { get; set; } = 5000;
        public bool PortChecking { get; set; }
        public long ServerId { get;  protected set;}
        public bool WarnOnCantKeepUp { get; protected set; }
        public IPEndPoint BindAddress { get; protected set;}
        public HookManager HookManager { get; protected set; }
        public void AddTask(long runIn, Action action)
        {
            lock (_tasks)
            {
                var ti = new TaskInfo
                {
                    RunIn = runIn,
                    RegisteredAt = ServerTime.ElapsedMilliseconds
                };
                if (!_tasks.ContainsKey(ti))
                {
                    _tasks.Add(ti, action);
                }
            }
        }

        public void AddPacketToQueue(RakNetPacket packet, IPEndPoint address)
        {
            lock (_sendQueue)
            {
                var buffer = packet.Encode();
                _sendQueue.Enqueue(new DatagramPacket(buffer, address.Address.ToString(), address.Port));
            }
        }
        
        public void AddShutdownTask(Action r)
        {
            lock (_shutdownTasks)
            {
                _shutdownTasks.Add(r);
            }
        }

        protected abstract int Send(byte[] data, int length, IPEndPoint endPoint);
        
        private void HandlePackets()
        {
            while (_sendQueue.Count != 0)
            {
                var pkt = _sendQueue.Dequeue();
                try
                {
                    var data = pkt.GetData();
                    Send(data, data.Length, pkt.Endpoint);
                }
                catch (IOException e)
                {
                    Log.Warn($"Exception while sending packet: {e.ToString()}");
                    //this.logger.warn("java.io.IOException while sending packet: " + e.getMessage());
                }
            }
            AddTask(0, HandlePackets); //Run next tick
        }
        
        private void Tick()
        {
            if (this._tasks.Count == 0) return;
            
            lock (this._tasks)
            {
                var remove = new List<TaskInfo>();
                var tasks = new Dictionary<TaskInfo, Action>(this._tasks);
                foreach (var ti in tasks.Keys.Where(ti => ServerTime.ElapsedMilliseconds - ti.RegisteredAt >= ti.RunIn))
                {
                    try
                    {
                        this._tasks[ti].Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Info("Exception: " + ex);
                    }
                    remove.Add(ti);
                }
                foreach (var i in remove)
                {
                    _tasks.Remove(i);
                }
            }
        }

        protected abstract bool Bind();
        public abstract void Start();
        public abstract void Stop();

        protected virtual void Run()
        {
            ServerTime.Restart();
            //this.logger.info("Server starting...");
            Log.Info("Starting...");
            if (Bind())
            {
                Log.Info("RakNet bound to " + BindAddress + ", running on RakNet protocol " +
                         JRakLibPlus.RaknetProtocol);

                try
                {
                    while (Running)
                    {
                        var start = ServerTime.ElapsedMilliseconds;
                        Tick();
                        var elapsed = ServerTime.ElapsedMilliseconds - start;
						
                        if (elapsed >= 50)
                        {
                            if (WarnOnCantKeepUp)
                                Log.Warn("Can't keep up, did the system time change or is the server overloaded? (" + elapsed + ">50)");
                        }
                        else
                        {
                            Thread.Sleep((int) (50 - elapsed));
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Fatal Exception, RakNet has crashed! " + e.Source + ": " + e);
                    Stop();
                }
            }

            lock (_shutdownTasks)
            {
                foreach (var i in _shutdownTasks)
                {
                    i.Invoke();
                }
            }

            Stopped = true;
            Log.Info("Stopped.");
        }
    }
}