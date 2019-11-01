using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MiniUDP
{
    internal class NetController
    {
        /// <summary>
        /// Deallocates a pool-spawned event.
        /// </summary>
        internal void RecycleEvent(NetEvent evnt)
        {
            eventPool.Deallocate(evnt);
        }

        #region Main Thread
        // This region should only be accessed by the MAIN thread

        /// <summary>
        /// Queues a notification to be sent to the given peer.
        /// Deep-copies the user data given.
        /// </summary>
        internal void QueueNotification(NetPeer target, byte[] buffer, ushort length)
        {
            NetEvent notification =
              CreateEvent(
                NetEventType.Notification,
                target);

            if (notification.ReadData(buffer, 0, length) == false)
            {
                throw new OverflowException("Data too long for notification");
            }

            notificationIn.Enqueue(notification);
        }

        /// <summary>
        /// Returns the first event on the background thread's outgoing queue.
        /// </summary>
        internal bool TryReceiveEvent(out NetEvent received)
        {
            return eventOut.TryDequeue(out received);
        }

        /// <summary>
        /// Queues up a request to connect to an endpoint.
        /// Returns the peer representing this pending connection.
        /// </summary>
        internal NetPeer BeginConnect(IPEndPoint endpoint, string token)
        {
            NetPeer peer = new NetPeer(endpoint, token, false, 0);
            connectIn.Enqueue(peer);
            return peer;
        }

        /// <summary>
        /// Optionally binds our socket before starting.
        /// </summary>
        internal void Bind(int port)
        {
            socket.Bind(port);
        }

        /// <summary>
        /// Signals the controller to begin.
        /// </summary>
        internal void Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException(
          "Controller has already been started");
            }

            isStarted = true;
            isRunning = true;

            Run();
        }

        /// <summary>
        /// Signals the controller to stop updating.
        /// </summary>
        internal void Stop()
        {
            isRunning = false;
        }

        /// <summary>
        /// Force-closes the socket, even if we haven't stopped running.
        /// </summary>
        internal void Close()
        {
            socket.Close();
        }

        /// <summary>
        /// Immediately sends out a disconnect message to a peer.
        /// Can be called on either thread.
        /// </summary>
        internal void SendKick(NetPeer peer, byte reason)
        {
            sender.SendKick(peer, NetCloseReason.KickUserReason, reason);
        }

        /// <summary>
        /// Immediately sends out a payload to a peer.
        /// Can be called on either thread.
        /// </summary>
        internal SocketError SendPayload(
          NetPeer peer,
          ushort sequence,
          byte[] data,
          ushort length)
        {
            return sender.SendPayload(peer, sequence, data, length);
        }
        #endregion

        #region Background Thread
        // This region should only be accessed by the BACKGROUND thread

        private bool IsFull => false;  // TODO: Keep a count
        private long Time => timer.ElapsedMilliseconds;

        private readonly NetPipeline<NetPeer> connectIn;
        private readonly NetPipeline<NetEvent> notificationIn;
        private readonly NetPipeline<NetEvent> eventOut;

        private readonly NetPool<NetEvent> eventPool;
        private readonly Dictionary<IPEndPoint, NetPeer> peers;
        private readonly Stopwatch timer;

        private readonly NetSocket socket;
        private readonly NetSender sender;
        private readonly NetReceiver receiver;
        private readonly string version;

        private readonly Queue<NetEvent> reusableQueue;
        private readonly List<NetPeer> reusableList;
        private readonly byte[] reusableBuffer;

        private long nextTick;
        private long nextLongTick;
        private bool isStarted;
        private bool isRunning;
        private readonly bool acceptConnections;

        internal NetController(
          string version,
          bool acceptConnections)
        {
            connectIn = new NetPipeline<NetPeer>();
            notificationIn = new NetPipeline<NetEvent>();
            eventOut = new NetPipeline<NetEvent>();

            eventPool = new NetPool<NetEvent>();
            peers = new Dictionary<IPEndPoint, NetPeer>();
            timer = new Stopwatch();
            socket = new NetSocket();
            sender = new NetSender(socket);
            receiver = new NetReceiver(socket);

            reusableQueue = new Queue<NetEvent>();
            reusableList = new List<NetPeer>();
            reusableBuffer = new byte[NetConfig.SOCKET_BUFFER_SIZE];

            nextTick = 0;
            nextLongTick = 0;
            isStarted = false;
            isRunning = false;
            this.acceptConnections = acceptConnections;

            this.version = version;
        }

        /// <summary>
        /// Controller's main update loop.
        /// </summary>
        private void Run()
        {
            timer.Start();
            while (isRunning)
            {
                Update();
                Thread.Sleep(NetConfig.SleepTime);
            }

            // Cleanup all peers since the loop was broken
            foreach (NetPeer peer in GetPeers())
            {
                bool sendEvent = peer.IsOpen;
                ClosePeer(peer, NetCloseReason.KickShutdown);

                if (sendEvent)
                {
                    eventOut.Enqueue(
            CreateClosedEvent(peer, NetCloseReason.LocalShutdown));
                }
            }
        }

        #region Peer Management
        /// <summary>
        /// Primary update logic. Iterates through and manages all peers.
        /// </summary>
        private void Update()
        {
#if DEBUG
            receiver.Update();
#endif

            ReadPackets();
            ReadNotifications();
            ReadConnectRequests();

            if (TickAvailable(out bool longTick))
            {
                foreach (NetPeer peer in GetPeers())
                {
                    peer.Update(Time);
                    switch (peer.Status)
                    {
                        case NetPeerStatus.Connecting:
                            UpdateConnecting(peer);
                            break;

                        case NetPeerStatus.Connected:
                            UpdateConnected(peer, longTick);
                            break;

                        case NetPeerStatus.Closed:
                            UpdateClosed(peer);
                            break;
                    }
                }
            }

#if DEBUG
            sender.Update();
#endif
        }

        /// <summary>
        /// Returns true iff it's time for a tick, or a long tick.
        /// </summary>
        private bool TickAvailable(out bool longTick)
        {
            longTick = false;
            long currentTime = Time;
            if (currentTime >= nextTick)
            {
                nextTick = currentTime + NetConfig.ShortTickRate;
                if (currentTime >= nextLongTick)
                {
                    longTick = true;
                    nextLongTick = currentTime + NetConfig.LongTickRate;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Receives pending outgoing notifications from the main thread 
        /// and assigns them to their recipient peers
        /// </summary>
        private void ReadNotifications()
        {
            NetEvent notification = null;
            while (notificationIn.TryDequeue(out notification))
            {
                if (notification.Peer.IsOpen)
                {
                    notification.Peer.QueueNotification(notification);
                }
            }
        }

        /// <summary>
        /// Read all connection requests and instantiate them as connecting peers.
        /// </summary>
        private void ReadConnectRequests()
        {
            while (connectIn.TryDequeue(out NetPeer pending))
            {
                if (peers.ContainsKey(pending.EndPoint))
                {
                    throw new ApplicationException("Connecting to existing peer");
                }

                if (pending.IsClosed) // User closed peer before we could connect
                {
                    continue;
                }

                peers.Add(pending.EndPoint, pending);
                pending.OnReceiveOther(Time);
            }
        }

        /// <summary>
        /// Updates a peer that is attempting to connect.
        /// </summary>
        private void UpdateConnecting(NetPeer peer)
        {
            if (peer.GetTimeSinceRecv(Time) > NetConfig.ConnectionTimeOut)
            {
                ClosePeerSilent(peer);
                eventOut.Enqueue(
                  CreateClosedEvent(peer, NetCloseReason.LocalTimeout));
                return;
            }

            sender.SendConnect(peer, version);
        }

        /// <summary>
        /// Updates a peer with an active connection.
        /// </summary>
        private void UpdateConnected(NetPeer peer, bool longTick)
        {
            if (peer.GetTimeSinceRecv(Time) > NetConfig.ConnectionTimeOut)
            {
                ClosePeer(peer, NetCloseReason.KickTimeout);
                eventOut.Enqueue(
                  CreateClosedEvent(peer, NetCloseReason.LocalTimeout));
                return;
            }

            long time = Time;
            if (peer.HasNotifications || peer.AckRequested)
            {
                sender.SendNotifications(peer);
                peer.AckRequested = false;
            }
            if (longTick)
            {
                sender.SendPing(peer, Time);
            }
        }

        /// <summary>
        /// Updates a peer that has been closed.
        /// </summary>
        private void UpdateClosed(NetPeer peer)
        {
            // The peer must have been closed by the main thread, because if
            // we closed it on this thread it would have been removed immediately
            NetDebug.Assert(peer.ClosedByUser);
            peers.Remove(peer.EndPoint);
        }

        /// <summary>
        /// Closes a peer, sending out a best-effort notification and removing
        /// it from the dictionary of active peers.
        /// </summary>
        private void ClosePeer(
          NetPeer peer,
          NetCloseReason reason)
        {
            if (peer.IsOpen)
            {
                sender.SendKick(peer, reason);
            }

            ClosePeerSilent(peer);
        }

        /// <summary>
        /// Closes a peer without sending a network notification.
        /// </summary>
        private void ClosePeerSilent(NetPeer peer)
        {
            if (peer.IsOpen)
            {
                peer.Disconnected();
                peers.Remove(peer.EndPoint);
            }
        }
        #endregion

        #region Packet Read
        /// <summary>
        /// Polls the socket and receives all pending packet data.
        /// </summary>
        private void ReadPackets()
        {
            for (int i = 0; i < NetConfig.MaxPacketReads; i++)
            {
                SocketError result =
                  receiver.TryReceive(out IPEndPoint source, out byte[] buffer, out int length);
                if (NetSocket.Succeeded(result) == false)
                {
                    return;
                }

                NetPacketType type = NetEncoding.GetType(buffer);
                if (type == NetPacketType.Connect)
                {
                    // We don't have a peer yet -- special case
                    HandleConnectRequest(source, buffer, length);
                }
                else
                {
                    if (peers.TryGetValue(source, out NetPeer peer))
                    {
                        switch (type)
                        {
                            case NetPacketType.Accept:
                                HandleConnectAccept(peer, buffer, length);
                                break;

                            case NetPacketType.Kick:
                                HandleKick(peer, buffer, length);
                                break;

                            case NetPacketType.Ping:
                                HandlePing(peer, buffer, length);
                                break;

                            case NetPacketType.Pong:
                                HandlePong(peer, buffer, length);
                                break;

                            case NetPacketType.Carrier:
                                HandleCarrier(peer, buffer, length);
                                break;

                            case NetPacketType.Payload:
                                HandlePayload(peer, buffer, length);
                                break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Protocol Handling
        /// <summary>
        /// Handles an incoming connection request from a remote peer.
        /// </summary>
        private void HandleConnectRequest(
          IPEndPoint source,
          byte[] buffer,
          int length)
        {
            bool success =
              NetEncoding.ReadConnectRequest(
                buffer,
                out string version,
                out string token);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading connect request");
                return;
            }

            if (ShouldCreatePeer(source, version))
            {
                long curTime = Time;
                // Create and add the new peer as a client
                NetPeer peer = new NetPeer(source, token, true, curTime);
                peers.Add(source, peer);
                peer.OnReceiveOther(curTime);

                // Accept the connection over the network
                sender.SendAccept(peer);

                // Queue the event out to the main thread to receive the connection
                eventOut.Enqueue(
                  CreateEvent(NetEventType.PeerConnected, peer));
            }
        }

        private void HandleConnectAccept(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            NetDebug.Assert(peer.IsClient == false, "Ignoring accept from client");
            if (peer.IsConnected || peer.IsClient)
            {
                return;
            }

            peer.OnReceiveOther(Time);
            peer.Connected();

            eventOut.Enqueue(
              CreateEvent(NetEventType.PeerConnected, peer));
        }

        private void HandleKick(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            if (peer.IsClosed)
            {
                return;
            }

            bool success =
              NetEncoding.ReadProtocol(
                buffer,
                length,
                out byte rawReason,
                out byte userReason);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading kick");
                return;
            }

            NetCloseReason closeReason = (NetCloseReason)rawReason;
            // Skip the packet if it's a bad reason (this will cause error output)
            if (NetUtil.ValidateKickReason(closeReason) == NetCloseReason.INVALID)
            {
                return;
            }

            peer.OnReceiveOther(Time);
            ClosePeerSilent(peer);
            eventOut.Enqueue(
              CreateClosedEvent(peer, closeReason, userReason));
        }

        private void HandlePing(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            if (peer.IsConnected == false)
            {
                return;
            }

            bool success =
              NetEncoding.ReadProtocol(
                buffer,
                length,
                out byte pingSeq,
                out byte loss);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading ping");
                return;
            }

            peer.OnReceivePing(Time, loss);
            sender.SendPong(peer, pingSeq, peer.GenerateDrop());
        }

        private void HandlePong(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            if (peer.IsConnected == false)
            {
                return;
            }

            bool success =
              NetEncoding.ReadProtocol(
                buffer,
                length,
                out byte pongSeq,
                out byte drop);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading pong");
                return;
            }

            peer.OnReceivePong(Time, pongSeq, drop);
        }

        private void HandleCarrier(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            if (peer.IsConnected == false)
            {
                return;
            }

            // Read the carrier and notifications
            reusableQueue.Clear();
            bool success =
              NetEncoding.ReadCarrier(
                CreateEvent,
                peer,
                buffer,
                length,
                out ushort notificationAck,
                out ushort notificationSeq,
                reusableQueue);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading carrier");
                return;
            }

            long curTime = Time;
            peer.OnReceiveCarrier(curTime, notificationAck, RecycleEvent);

            // The packet contains the first sequence number. All subsequent
            // notifications have sequence numbers in order, so we just increment.
            foreach (NetEvent notification in reusableQueue)
            {
                if (peer.OnReceiveNotification(curTime, notificationSeq++))
                {
                    eventOut.Enqueue(notification);
                }
            }
        }

        private void HandlePayload(
          NetPeer peer,
          byte[] buffer,
          int length)
        {
            if (peer.IsConnected == false)
            {
                return;
            }

            // Read the payload
            bool success =
              NetEncoding.ReadPayload(
                CreateEvent,
                peer,
                buffer,
                length,
                out ushort payloadSeq,
                out NetEvent evnt);

            // Validate
            if (success == false)
            {
                NetDebug.LogError("Error reading payload");
                return;
            }

            // Enqueue the event for processing if the peer can receive it
            if (peer.OnReceivePayload(Time, payloadSeq))
            {
                eventOut.Enqueue(evnt);
            }
        }
        #endregion

        #region Event Allocation
        private NetEvent CreateEvent(
          NetEventType type,
          NetPeer target)
        {
            NetEvent evnt = eventPool.Allocate();
            evnt.Initialize(
              type,
              target);
            return evnt;
        }

        private NetEvent CreateClosedEvent(
          NetPeer target,
          NetCloseReason closeReason,
          byte userKickReason = 0,
          SocketError socketError = SocketError.SocketError)
        {
            NetEvent evnt = CreateEvent(NetEventType.PeerClosed, target);
            evnt.CloseReason = closeReason;
            evnt.UserKickReason = userKickReason;
            evnt.SocketError = socketError;
            return evnt;
        }
        #endregion

        #region Misc. Helpers
        /// <summary>
        /// Whether or not we should accept a connection before consulting
        /// the application for the final verification step.
        /// 
        /// TODO: Should we create a peer anyway temporarily and include it in
        ///       cross-thread queue event so the main thread knows we rejected
        ///       a connection attempt for one of these reasons?
        /// </summary>
        private bool ShouldCreatePeer(
          IPEndPoint source,
          string version)
        {
            if (peers.TryGetValue(source, out NetPeer peer))
            {
                sender.SendAccept(peer);
                return false;
            }

            if (acceptConnections == false)
            {
                sender.SendReject(source, NetCloseReason.RejectNotHost);
                return false;
            }

            if (IsFull)
            {
                sender.SendReject(source, NetCloseReason.RejectFull);
                return false;
            }

            if (this.version != version)
            {
                sender.SendReject(source, NetCloseReason.RejectVersion);
                return false;
            }

            return true;
        }

        private IEnumerable<NetPeer> GetPeers()
        {
            reusableList.Clear();
            reusableList.AddRange(peers.Values);
            return reusableList;
        }
        #endregion

        #endregion
    }
}
