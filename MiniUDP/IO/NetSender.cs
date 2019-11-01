using System.Net;
using System.Net.Sockets;

namespace MiniUDP
{
    /// <summary>
    /// Threadsafe class for writing and sending data via a socket.
    /// </summary>
    internal class NetSender
    {
        private readonly object sendLock = new object();
        private readonly byte[] sendBuffer = new byte[NetConfig.SOCKET_BUFFER_SIZE];
        private readonly NetSocket socket;

        internal NetSender(NetSocket socket)
        {
            this.socket = socket;
        }

        /// <summary>
        /// Sends a kick (reject) message to an unconnected peer.
        /// </summary>
        internal SocketError SendReject(IPEndPoint destination, NetCloseReason reason)
        {
            // Skip the packet if it's a bad reason (this will cause error output)
            if (NetUtil.ValidateKickReason(reason) == NetCloseReason.INVALID)
            {
                return SocketError.Success;
            }

            lock (sendLock)
            {
                var length = NetEncoding.PackProtocol(sendBuffer, NetPacketType.Kick, (byte)reason, 0);
                return TrySend(destination, sendBuffer, length);
            }
        }

        /// <summary>
        /// Sends a request to connect to a remote peer.
        /// </summary>
        internal SocketError SendConnect(NetPeer peer, string version)
        {
            lock (sendLock)
            {
                var length = NetEncoding.PackConnectRequest(sendBuffer, version, peer.Token);
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Accepts a remote request and sends an affirmative reply.
        /// </summary>
        internal SocketError SendAccept(NetPeer peer)
        {
            lock (sendLock)
            {
                var length = NetEncoding.PackProtocol(sendBuffer, NetPacketType.Accept, 0, 0);
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Notifies a peer that we are disconnecting. May not arrive.
        /// </summary>
        internal SocketError SendKick(NetPeer peer, NetCloseReason reason, byte userReason = 0)
        {
            // Skip the packet if it's a bad reason (this will cause error output)
            if (NetUtil.ValidateKickReason(reason) == NetCloseReason.INVALID)
            {
                return SocketError.Success;
            }

            lock (sendLock)
            {
                var length = NetEncoding.PackProtocol(sendBuffer, NetPacketType.Kick, (byte)reason, userReason);
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Sends a generic ping packet.
        /// </summary>
        internal SocketError SendPing(NetPeer peer, long curTime)
        {
            lock (sendLock)
            {
                var length = NetEncoding.PackProtocol(sendBuffer, NetPacketType.Ping, peer.GeneratePing(curTime), peer.GenerateLoss());
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Sends a generic pong packet.
        /// </summary>
        internal SocketError SendPong(NetPeer peer, byte pingSeq, byte drop)
        {
            lock (sendLock)
            {
                var length = NetEncoding.PackProtocol(sendBuffer, NetPacketType.Pong, pingSeq, drop);
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Sends a scheduled notification message.
        /// </summary>
        internal SocketError SendNotifications(NetPeer peer)
        {
            lock (sendLock)
            {
                var packedLength = NetEncoding.PackCarrier(sendBuffer, peer.NotificationAck, peer.GetFirstSequence(), peer.Outgoing);
                var length = packedLength;
                return TrySend(peer.EndPoint, sendBuffer, length);
            }
        }

        /// <summary>
        /// Immediately sends out a payload to a peer.
        /// </summary>
        internal SocketError SendPayload(NetPeer peer, ushort sequence, byte[] data, ushort dataLength)
        {
            lock (sendLock)
            {
                var size = NetEncoding.PackPayload(sendBuffer, sequence, data, dataLength);
                return TrySend(peer.EndPoint, sendBuffer, size);
            }
        }

        /// <summary>
        /// Sends a packet over the network.
        /// </summary>
        private SocketError TrySend(IPEndPoint endPoint, byte[] buffer, int length)
        {
#if DEBUG
            if (NetConfig.LatencySimulation)
            {
                outQueue.Enqueue(endPoint, buffer, length);
                return SocketError.Success;
            }
#endif
            return socket.TrySend(endPoint, buffer, length);
        }

        // #region Latency Simulation
#if DEBUG
        private readonly NetDelay outQueue = new NetDelay();

        internal void Update()
        {
            lock (sendLock)
            {
                while (outQueue.TryDequeue(out IPEndPoint endPoint, out byte[] buffer, out int length))
                {
                    socket.TrySend(endPoint, buffer, length);
                }
            }
        }
#endif
        // #endregion
    }
}
