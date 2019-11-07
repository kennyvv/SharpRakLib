using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.Minecraft;
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
        
        private readonly ConcurrentQueue<int> _ackQueue = new ConcurrentQueue<int>();
        protected long _clientId;

        private int _lastSeqNum = -1;

        private int _messageIndex;
        protected short _mtu;

        public short MTU => _mtu;
        
        private readonly List<int> _nackQueue = new List<int>();
        protected readonly Dictionary<int, CustomPacket> _recoveryQueue = new Dictionary<int, CustomPacket>();

        private CustomPacket _sendQueue = new CustomPackets.CustomPacket4();
        private int _sendSeqNum;
        private int _splitId;

        private readonly Dictionary<int, Dictionary<int, EncapsulatedPacket>> _splitQueue =
            new Dictionary<int, Dictionary<int, EncapsulatedPacket>>();

        public int _state;
        protected long _timeLastPacketReceived;
        
        public IPEndPoint Address { get; }
        protected ISessionManager SessionManager { get; }
        public SessionBase(IPEndPoint address, ISessionManager manager, short mtu)
        {
            this.Address = address;
            _state = Connecting1;
            SessionManager = manager;
            _mtu = mtu;
            _state = Connecting1;
            
            SessionManager.AddTask(0, Tick);
        }

        public virtual void Disconnect(string reason)
        {
            Log.Warn($"Disconnect: {reason}");
        }
        
        private void Tick()
        {
            if (_state == Disconnected) return;
            
            if (SessionManager.Runtime - _timeLastPacketReceived >= SessionManager.PacketTimeout)
            {
                Disconnect("timeout");
            }
            else
            {

                var ackCount = _ackQueue.Count;
                if (ackCount != 0)
                {
                    List<int> acks = new List<int>();

                    for (int i = 0; i < ackCount; i++)
                    {
                        if (_ackQueue.TryDequeue(out var value))
                        {
                            acks.Add(value);
                        }
                    }

                    if (acks.Count > 0)
                    {
                        var ack = new AckPacket();
                        ack.Packets = acks.ToArray();
                        SendPacket(ack);
                        
                      //  Console.WriteLine($"Sending acks.");
                    }
                }

                lock (_nackQueue)
                {
                    var nacks = _nackQueue.ToArray();
                    _nackQueue.Clear();
                    
                    if (nacks.Length > 0)
                    {
                        var nack = new NackPacket();
                        nack.Packets = nacks;
                        SendPacket(nack);
                    }
                }

                SendQueuedPackets();

                SessionManager.AddTask(1, Tick);
            }
        }

        private object _queueLock = new object();
        private void SendQueuedPackets()
        {
            CustomPacket queue;
            lock (_queueLock)
            {
                queue = _sendQueue;

                if (queue.Packets.Count == 0)
                {
                    return;
                }
                
                _sendQueue = new CustomPackets.CustomPacket0();
            }

            queue.SequenceNumber = Interlocked.Increment(ref _sendSeqNum);
            SendPacket(queue);

            lock (_recoveryQueue)
            {
                _recoveryQueue.Add(queue.SequenceNumber, queue);
            }
        }

        protected void SendPacket(RakNetPacket packet)
        {
            SessionManager.AddPacketToQueue(packet, Address);
        }

        private static IEnumerable<byte[]> ArraySplit(byte[] bArray, int intBufforLengt)
        {
            int bArrayLenght = bArray.Length;
            byte[] bReturn;

            int i = 0;
            for (; bArrayLenght > (i + 1) * intBufforLengt; i++)
            {
                bReturn = new byte[intBufforLengt];

                Buffer.BlockCopy(bArray, i * intBufforLengt, bReturn, 0, intBufforLengt);
                yield return bReturn;
            }

            int intBufforLeft = bArrayLenght - i * intBufforLengt;
            if (intBufforLeft > 0)
            {
                bReturn = new byte[intBufforLeft];

                Buffer.BlockCopy(bArray, i * intBufforLengt, bReturn, 0, intBufforLeft);
                yield return bReturn;
            }
        }
        
        public void AddPacketToQueue(EncapsulatedPacket pkt, bool immediate)
        {
            switch (pkt.Reliability)
            {
                case Reliability.Reliable:
                case Reliability.ReliableOrdered:
                //TODO: OrderIndex
                case Reliability.ReliableSequenced:
                case Reliability.ReliableWithAckReceipt:
                case Reliability.ReliableOrderedWithAckReceipt:
                    pkt.MessageIndex = Interlocked.Increment(ref _messageIndex);
                    break;
            }

            var packetSize = pkt.Encode().Length;
            if (packetSize + 4 > _mtu)
            {
                
                // Too big to be sent in one packet, need to be split
                var buffers = ArraySplit(pkt.Payload, _mtu - 34).ToArray();

                var splitId = Interlocked.Increment(ref _splitId) % 65536;
                for (var count = 0; count < buffers.Length; count++)
                {
                    var ep = new EncapsulatedPacket();
                    ep.SplitId = splitId;
                    ep.Split = true;
                    ep.SplitCount = buffers.Length;
                    ep.Reliability = pkt.Reliability;
                    ep.SplitIndex = count;
                    ep.Payload = buffers[count];

                    if (count > 0)
                    {
                        ep.MessageIndex = Interlocked.Increment(ref _messageIndex);
                    }
                    else
                    {
                        ep.MessageIndex = pkt.MessageIndex;
                    }
                    if (ep.Reliability == Reliability.ReliableOrdered)
                    {
                        ep.OrderChannel = pkt.OrderChannel;
                        ep.OrderIndex = pkt.OrderIndex;
                    }

                    AddToQueue(ep, true);
                }
            }
            else
            {
                AddToQueue(pkt, immediate);
            }
        }
        
        private void AddToQueue(EncapsulatedPacket pkt, bool immediate)
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
            
            var ack = new AckPacket();
            ack.Packets = new []{pk.SequenceNumber};
            SendPacket(ack);
            
            //_ackQueue.Enqueue(pk.SequenceNumber);
            

            if (diff >= 1)
            {
                _lastSeqNum = pk.SequenceNumber;
            }
            
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
                    if (!m.ContainsKey(pk.SplitIndex))
                        m.Add(pk.SplitIndex, pk);
                    
                    _splitQueue[pk.SplitId] = m;
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
                    
                    HandleEncapsulatedPacket(ep);
                }
            }
        }
    }
}