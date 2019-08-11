using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using NLog;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SharpRakLib.Core
{
    public class SessionBase
    {
        private static ILogger Log = LogManager.GetCurrentClassLogger();
        
        public const int Disconnected = 0;
        public const int Connecting1 = 1;
        public const int Connecting2 = 2;
        public const int Handshaking = 3;
        public const int Connected = 4;

        public const int MaxSplitSize = 128;
        public const int MaxSplitCount = 4;
        
        private readonly List<int> _ackQueue = new List<int>();
        protected long _clientId;

        private int _lastSeqNum = -1;

        private int _messageIndex;
        protected short _mtu;
        private readonly List<int> _nackQueue = new List<int>();
        protected readonly Dictionary<int, CustomPacket> _recoveryQueue = new Dictionary<int, CustomPacket>();

        private CustomPacket _sendQueue = new CustomPackets.CustomPacket4();
        private int _sendSeqNum;
        private int _splitId;

        private readonly Dictionary<int, Dictionary<int, EncapsulatedPacket>> _splitQueue =
            new Dictionary<int, Dictionary<int, EncapsulatedPacket>>();

        public int _state;
        private long _timeLastPacketReceived;
        
        public IPEndPoint Address { get; }
        protected ISessionManager SessionManager { get; }
        public SessionBase(IPEndPoint address, ISessionManager manager, short mtu)
        {
            this.Address = address;
            _state = Connecting1;
            SessionManager = manager;
            _mtu = mtu;

            SessionManager.AddTask(0, Update);
        }

        public virtual void Disconnect(string reason)
        {
            Log.Warn($"Disconnect: {reason}");
        }
        
        private void Update()
        {
            if (_state == Disconnected) return;
            
            if (SessionManager.Runtime - _timeLastPacketReceived >= SessionManager.PacketTimeout)
            {
                Disconnect("timeout");
            }
            else
            {
                lock (_ackQueue)
                {
                    if (_ackQueue.Count != 0)
                    {
                        var ack = new AckPacket();
                        ack.Packets = _ackQueue.ToArray();
                        SendPacket(ack);
                        _ackQueue.Clear();
                    }
                }
                lock (_nackQueue)
                {
                    if (_nackQueue.Count != 0)
                    {
                        var nack = new NackPacket();
                        nack.Packets = _nackQueue.ToArray();
                        SendPacket(nack);
                        _nackQueue.Clear();
                    }
                }

                SendQueuedPackets();

                SessionManager.AddTask(0, Update);
            }
        }

        private object _queueLock = new object();
        private void SendQueuedPackets()
        {
            CustomPacket queue;
            lock (_queueLock)
            {
                queue = _sendQueue;

                if (queue.Packets.Count != 0)
                {
                    _sendQueue = new CustomPackets.CustomPacket4();
                }
                else
                {
                    return;
                }
            }

            queue.SequenceNumber = Interlocked.Increment(ref _sendSeqNum);
            SendPacket(queue);
            
            Log.Info($"Sending queue: {queue.Packets.Count}");
            
            lock (_recoveryQueue)
            {
                _recoveryQueue.Add(queue.SequenceNumber, queue);
            }
        }

        protected void SendPacket(RakNetPacket packet)
        {
            Log.Info($"Sending: {packet.ToString()} | MTU: {_mtu}");
			
            SessionManager.AddPacketToQueue(packet, Address);
        }

        public void AddToQueue(EncapsulatedPacket pkt, bool immediate)
        {
            if (immediate)
            {
                CustomPacket cp = new CustomPackets.CustomPacket0();
                cp.Packets.Add(pkt);
                cp.SequenceNumber = Interlocked.Increment(ref _sendSeqNum);
                SendPacket(cp);
                lock (_recoveryQueue)
                {
                    _recoveryQueue.Add(cp.SequenceNumber, cp);
                }
                Log.Info($"Sent immediate: {pkt.ToString()} | {(DefaultMessageIdTypes) pkt.Payload[0]}");
            }
            else
            {
                lock (_queueLock)
                {
                    if (_sendQueue.GetSize() + pkt.GetSize() > _mtu)
                    {
                        SendQueuedPackets();
                    }
                    
                    _sendQueue.Packets.Add(pkt);
                    Log.Info($"Queued: {pkt.ToString()} | {(DefaultMessageIdTypes) pkt.Payload[0]} | MTU: {_mtu} | Queued: {_sendQueue.Packets.Count}");
                }
            }
        }

        public void ProcessPacket(byte[] data)
        {
            if (_state == Disconnected) return;
            _timeLastPacketReceived = SessionManager.Runtime;
            
            switch (data[0])
            {
                // ACK/NACK
                case JRakLibPlus.Ack:
                    if (_state != Connected || _state == Handshaking)
                    {
                        Log.Warn($"Got ACK in state: {_state}");
                        break;
                    }
                    var ack = new AckPacket();
                    ack.Decode(data);
                    
                    Log.Info($"Got ACK");
                    
                    lock (_recoveryQueue)
                    {
                        foreach (var seq in ack.Packets)
                        {
                            if (_recoveryQueue.ContainsKey(seq))
                            {
                                _recoveryQueue.Remove(seq);
                            }
                        }
                    }

                    break;
                case JRakLibPlus.Nack:
                    if (_state != Connected || _state == Handshaking)
                    {
                        Log.Warn($"Got NACK in state: {_state}");
                        break;
                    }
                    
                    var nack = new NackPacket();
                    nack.Decode(data);

                    Log.Info($"Got NACK");
                    
                    lock (_recoveryQueue)
                    {
                        foreach (var seq in nack.Packets)
                        {
                            if (_recoveryQueue.ContainsKey(seq))
                            {
                                var pk = _recoveryQueue[seq];
                                pk.SequenceNumber = _sendSeqNum++;
                                SendPacket(pk);
                                _recoveryQueue.Remove(seq);
                            }
                        }
                    }
                    break;
                default:
                    HandlePacket(data);
                    break;
            }
            
            //HandlePacket(data);
        }
        
        protected virtual void HandlePacket(byte[] data)
        {
            
        }
        
        protected void HandleDataPacket(byte[] data)
        {
            _timeLastPacketReceived = SessionManager.Runtime;
            
            CustomPacket pk = new CustomPackets.CustomPacket0();
            pk.Decode(data);

            var diff = pk.SequenceNumber - _lastSeqNum;
            lock (_nackQueue)
            {
                if (_nackQueue.Count != 0)
                {
                    _nackQueue.Remove(pk.SequenceNumber);
                    if (diff != 1)
                    {
                        for (var i = _lastSeqNum + 1; i < pk.SequenceNumber; i++)
                        {
                            _nackQueue.Add(i);
                        }
                    }
                }
            }
            lock (_ackQueue)
            {
                _ackQueue.Add(pk.SequenceNumber);
            }

            if (diff >= 1)
            {
                _lastSeqNum = pk.SequenceNumber;
            }

            Log.Info($"Sequence: {pk.SequenceNumber} | Packets: {pk.Packets.Count}");
            pk.Packets.ForEach(HandleEncapsulatedPacket);
        }

        private void HandleEncapsulatedPacket(EncapsulatedPacket pk)
        {
            if (!(_state == Connected || _state == Handshaking))
                return;

            if (pk.Split && _state == Connected)
            {
                HandleSplitPacket(pk);
                return;
            }
            
            if (!HandleEncapsulated(pk))
            {
                //pk.Payload
                /*Log.Warn($"Payload (0x{pk.ReadId:X2}):");
                foreach (var i in pk.Payload)
                {
                    Console.Write(i.ToString("x2") + " ");
                }
                Console.WriteLine();
                Console.WriteLine();*/
                
                SessionManager.HookManager.ActivateHook(HookManager.Hook.PacketRecieved, this, pk);
            }
        }

        protected virtual bool HandleEncapsulated(EncapsulatedPacket packet)
        {
            return false;
        }

        private void HandleSplitPacket(EncapsulatedPacket pk)
        {
            if (pk.SplitCount >= MaxSplitSize || pk.SplitIndex >= MaxSplitSize || pk.SplitIndex < 0)
            {
                return;
            }

            lock (_splitQueue)
            {
                if (!_splitQueue.ContainsKey(pk.SplitId))
                {
                    if (_splitQueue.Count >= MaxSplitCount)
                        return; //Too many split packets in the queue will increase memory usage
                    var m = new Dictionary<int, EncapsulatedPacket>();
                    m.Add(pk.SplitIndex, pk);
                    _splitQueue.Add(pk.SplitId, m);
                }
                else
                {
                    var m = _splitQueue[pk.SplitId];
                    m.Add(pk.SplitIndex, pk);
                    _splitQueue.Add(pk.SplitId, m);
                }

                if (_splitQueue[pk.SplitId].Count == pk.SplitCount)
                {
                    var ep = new EncapsulatedPacket();
					
                    var packets = _splitQueue[pk.SplitId];
					
                    using (MemoryStream stream = new MemoryStream())
                    {
                        for (var i = 0; i < pk.SplitCount; i++)
                        {
                            stream.Write(packets[i].Payload);
                        }

                        ep.Payload = stream.ToArray();
                    }
                    _splitQueue.Remove(pk.SplitId);

                    Log.Info($"Read split packet");
                    HandleEncapsulatedPacket(ep);
                }
            }
        }
    }
}