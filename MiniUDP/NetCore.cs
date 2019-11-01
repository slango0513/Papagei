using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MiniUDP
{
    public delegate void NetPeerConnectEvent(NetPeer peer, string token);

    public delegate void NetPeerCloseEvent(NetPeer peer, NetCloseReason reason, byte userKickReason, SocketError error);

    public delegate void NetPeerPayloadEvent(NetPeer peer, byte[] data, ushort dataLength);

    public delegate void NetPeerNotificationEvent(NetPeer peer, byte[] data, ushort dataLength);

    public class NetCore
    {
        public event NetPeerConnectEvent PeerConnected;
        public event NetPeerCloseEvent PeerClosed;
        public event NetPeerPayloadEvent PeerPayload;
        public event NetPeerNotificationEvent PeerNotification;

        private readonly NetController controller;
        private Thread controllerThread;

        public NetCore(string version, bool allowConnections)
        {
            if (version == null)
            {
                version = "";
            }

            if (Encoding.UTF8.GetByteCount(version) > NetConfig.MAX_VERSION_BYTES)
            {
                throw new ApplicationException("Version string too long");
            }

            controller = new NetController(version, allowConnections);
        }

        public NetPeer Connect(IPEndPoint endpoint, string token)
        {
            NetPeer peer = AddConnection(endpoint, token);
            Start();
            return peer;
        }

        public void Host(int port)
        {
            controller.Bind(port);
            Start();
        }

        private void Start()
        {
            controllerThread = new Thread(new ThreadStart(controller.Start))
            {
                IsBackground = true
            };
            controllerThread.Start();
        }

        public NetPeer AddConnection(IPEndPoint endpoint, string token)
        {
            if (token == null)
            {
                token = "";
            }

            if (Encoding.UTF8.GetByteCount(token) > NetConfig.MAX_TOKEN_BYTES)
            {
                throw new ApplicationException("Token string too long");
            }

            var pending = controller.BeginConnect(endpoint, token);
            pending.SetCore(this);
            return pending;
        }

        public void Stop(int timeout = 1000)
        {
            controller.Stop();
            controllerThread.Join(timeout);
            controller.Close();
        }

        public void PollEvents()
        {
            while (controller.TryReceiveEvent(out NetEvent evnt))
            {
                var peer = evnt.Peer;

                // No events should fire if the user closed the peer
                if (peer.ClosedByUser == false)
                {
                    switch (evnt.EventType)
                    {
                        case NetEventType.PeerConnected:
                            peer.SetCore(this);
                            peer.OnPeerConnected();
                            PeerConnected?.Invoke(peer, peer.Token);
                            break;

                        case NetEventType.PeerClosed:
                            peer.OnPeerClosed(evnt.CloseReason, evnt.UserKickReason, evnt.SocketError);
                            PeerClosed?.Invoke(peer, evnt.CloseReason, evnt.UserKickReason, evnt.SocketError);
                            break;

                        case NetEventType.Payload:
                            peer.OnPayloadReceived(evnt.EncodedData, evnt.EncodedLength);
                            PeerPayload?.Invoke(peer, evnt.EncodedData, evnt.EncodedLength);
                            break;

                        case NetEventType.Notification:
                            peer.OnNotificationReceived(evnt.EncodedData, evnt.EncodedLength);
                            PeerNotification?.Invoke(peer, evnt.EncodedData, evnt.EncodedLength);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }

                controller.RecycleEvent(evnt);
            }
        }

        /// <summary>
        /// Immediately sends out a disconnect message to a peer.
        /// </summary>
        internal void SendKick(NetPeer peer, byte reason)
        {
            controller.SendKick(peer, reason);
        }

        /// <summary>
        /// Immediately sends out a payload to a peer.
        /// </summary>
        internal SocketError SendPayload(NetPeer peer, ushort sequence, byte[] data, ushort length)
        {
            return controller.SendPayload(peer, sequence, data, length);
        }

        /// <summary>
        /// Adds an outgoing notification to the controller processing queue.
        /// </summary>
        internal void QueueNotification(NetPeer peer, byte[] buffer, ushort length)
        {
            controller.QueueNotification(peer, buffer, length);
        }
    }
}
