using System;
using System.Net;
using SharpRakLib.Protocol;
using SharpRakLib.Server;

namespace SharpRakLib.Core
{
    public interface ISessionManager
    {
        bool Running { get; set; }
        bool Stopped { get; set; }
        //private Logger logger;
        string BroadcastName { get; set; }
        int MaxPacketsPerTick { get; set; }
        int ReceiveBufferSize { get; }
        int SendBufferSize { get; }
        int PacketTimeout { get; set; }
        bool PortChecking { get; set; }
        long ServerId { get; }
        bool WarnOnCantKeepUp { get; }

        IPEndPoint BindAddress { get; }
        HookManager HookManager { get; }
        
        void AddTask(long runIn, Action action);
        void AddPacketToQueue(RakNetPacket packet, IPEndPoint address);
    }
}