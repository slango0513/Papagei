using System;
using System.Net.Sockets;

namespace MiniUDP
{
    /// <summary>
    /// A multipurpose class (ab)used in two ways. Used for passing messages
    /// between threads internally (called "events" in this instance) on the 
    /// pipeline queues. Also encoded/decoded over the network to pass reliable 
    /// messages to connected peers (called "notifications" in this instance).
    /// </summary>
    internal class NetEvent : INetPoolable<NetEvent>
    {
        void INetPoolable<NetEvent>.Reset() { Reset(); }

        internal byte[] EncodedData => buffer;
        internal ushort EncodedLength => length;

        // Buffer for encoded user data
        private byte[] buffer = new byte[NetConfig.DATA_INITIAL];
        private ushort length = 0;

        // Additional data for passing events around internally, not synchronized
        internal NetEventType EventType { get; private set; }
        internal NetPeer Peer { get; private set; }  // Associated peer

        // Additional data, may or may not be set
        internal NetCloseReason CloseReason { get; set; }
        internal SocketError SocketError { get; set; }
        internal byte UserKickReason { get; set; }
        internal ushort Sequence { get; set; }

        public NetEvent()
        {
            Reset();
        }

        private void Reset()
        {
            length = 0;
            EventType = NetEventType.INVALID;
            Peer = null;

            CloseReason = NetCloseReason.INVALID;
            SocketError = SocketError.SocketError;
            UserKickReason = 0;
            Sequence = 0;
        }

        internal void Initialize(NetEventType type, NetPeer peer)
        {
            Reset();
            length = 0;
            EventType = type;
            Peer = peer;
        }

        internal bool ReadData(byte[] sourceBuffer, int position, ushort length)
        {
            if (length > NetConfig.DATA_MAXIMUM)
            {
                NetDebug.LogError("Data too long for NetEvent");
                return false;
            }

            // Resize if necessary
            var paddedLength = length + NetConfig.DATA_PADDING;
            if (buffer.Length < paddedLength)
            {
                buffer = new byte[paddedLength];
            }

            // Copy the contents
            Array.Copy(sourceBuffer, position, buffer, 0, length);
            this.length = length;
            return true;
        }
    }
}
