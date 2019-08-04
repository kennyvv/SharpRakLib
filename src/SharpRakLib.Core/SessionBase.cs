using System.Collections.Generic;
using System.Net;
using SharpRakLib.Protocol.RakNet;
using SharpRakLib.Server;

namespace SharpRakLib.Core
{
    public class SessionBase
    {
        private readonly List<int> _ackQueue = new List<int>();
        private long _clientId;

        private int _lastPing = -99;

        private int _lastSeqNum = -1;

        private int _messageIndex;
        private short _mtu;
        private readonly List<int> _nackQueue = new List<int>();
        private readonly Dictionary<int, CustomPacket> _recoveryQueue = new Dictionary<int, CustomPacket>();

        private readonly CustomPacket _sendQueue = new CustomPackets.CustomPacket4();
        private int _sendSeqNum;
        private int _splitId;

        private readonly Dictionary<int, Dictionary<int, EncapsulatedPacket>> _splitQueue =
            new Dictionary<int, Dictionary<int, EncapsulatedPacket>>();

        private int _state;
        private long _timeLastPacketReceived;
        
        public IPEndPoint Address { get; }

        public SessionBase()
        {
            _state = Session.Connecting1;
            this._server.AddTask(0, Update);
        }
        
        private void Update()
        {
            if (_state == Disconnected) return;
            if (JavaHelper.CurrentTimeMillis() - _timeLastPacketReceived >= _server.PacketTimeout)
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

                _server.AddTask(0, Update);
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
    }
}