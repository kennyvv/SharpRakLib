using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SharpRakLib.Protocol;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SharpRakLib.Core
{
    public class SessionBase
    {
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

        private readonly CustomPacket _sendQueue = new CustomPackets.CustomPacket4();
        private int _sendSeqNum;
        private int _splitId;

        private readonly Dictionary<int, Dictionary<int, EncapsulatedPacket>> _splitQueue =
            new Dictionary<int, Dictionary<int, EncapsulatedPacket>>();

        protected int _state;
        private long _timeLastPacketReceived;
        
        public IPEndPoint Address { get; }
        protected ISessionManager SessionManager { get; }
        public SessionBase(IPEndPoint address, ISessionManager manager)
        {
            this.Address = address;
            _state = Connecting1;
            SessionManager = manager;
            
            SessionManager.AddTask(0, Update);
        }

        public virtual void Disconnect(string reason)
        {
            
        }
        
        private void Update()
        {
            if (_state == Disconnected) return;
            if (JavaHelper.CurrentTimeMillis() - _timeLastPacketReceived >= SessionManager.PacketTimeout)
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
        
        private void SendQueuedPackets()
        {
            lock (_sendQueue)
            {
                if (_sendQueue.Packets.Count != 0)
                {
                    _sendQueue.SequenceNumber = _sendSeqNum++;
                    SendPacket(_sendQueue);
                    lock (_recoveryQueue)
                    {
                        _recoveryQueue.Add(_sendQueue.SequenceNumber, _sendQueue);
                    }

                    _sendQueue.Packets.Clear();
                }
            }
        }
        
        public void SendPacket(RakNetPacket packet)
        {
            Console.WriteLine($"Sending: {packet.ToString()} | MTU: {_mtu}");
			
            SessionManager.AddPacketToQueue(packet, Address);
        }
        
        protected void AddToQueue(EncapsulatedPacket pkt, bool immediate)
        {
            Console.WriteLine($"Queued: {pkt.ToString()} | {(DefaultMessageIdTypes) pkt.Payload[0]} | MTU: {_mtu}");
            if (immediate)
            {
                CustomPacket cp = new CustomPackets.CustomPacket0();
                cp.Packets.Add(pkt);
                cp.SequenceNumber = _sendSeqNum++;
                SendPacket(cp);
                lock (_recoveryQueue)
                {
                    _recoveryQueue.Add(cp.SequenceNumber, cp);
                }
            }
            else
            {
                if (_sendQueue.GetSize() + pkt.GetSize() > _mtu)
                {
                    SendQueuedPackets();
                }
                lock (_sendQueue)
                {
                    _sendQueue.Packets.Add(pkt);
                }
            }
        }

        public void ProcessPacket(byte[] data)
        {
            if (_state == Disconnected) return;
            _timeLastPacketReceived = JavaHelper.CurrentTimeMillis();

            switch (data[0])
            {
                // ACK/NACK

                case JRakLibPlus.Ack:
                    if (_state != Connected || _state == Handshaking) break;
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
                    if (_state != Connected || _state == Handshaking) break;
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
        }
        
        protected virtual void HandlePacket(byte[] data)
        {
            
        }
        
        protected void HandleDataPacket(byte[] data)
        {
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

            pk.Packets.ForEach(HandleEncapsulatedPacket);
        }

        private void HandleEncapsulatedPacket(EncapsulatedPacket pk)
        {
            if (!(_state == Connected || _state == Handshaking))
                return;
            
            if (pk.Split && _state == Connected)
            {
                HandleSplitPacket(pk);
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

                    HandleEncapsulatedPacket(ep);
                }
            }
        }
    }
}