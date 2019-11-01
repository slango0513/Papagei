using System.Net;
using System.Net.Sockets;

namespace MiniUDP
{
    /// <summary>
    /// A class for receiving and buffering data from a socket. Note that this
    /// class is NOT thread safe and must not be shared across threads.
    /// </summary>
    internal class NetReceiver
    {
        private readonly NetSocket socket;
        private readonly byte[] receiveBuffer = new byte[NetConfig.SOCKET_BUFFER_SIZE];

        internal NetReceiver(NetSocket socket)
        {
            this.socket = socket;
        }

        public SocketError TryReceive(out IPEndPoint source, out byte[] buffer, out int length)
        {
#if DEBUG
            if (NetConfig.LatencySimulation)
            {
                if (inQueue.TryDequeue(out source, out buffer, out length))
                {
                    return SocketError.Success;
                }

                return SocketError.NoData;
            }
#endif

            buffer = receiveBuffer;
            return socket.TryReceive(out source, receiveBuffer, out length);
        }

        // #region Latency Simulation
#if DEBUG
        private readonly NetDelay inQueue = new NetDelay();

        internal void Update()
        {
            if (NetConfig.LatencySimulation)
            {
                for (int i = 0; i < NetConfig.MaxPacketReads; i++)
                {
                    var result = socket.TryReceive(out IPEndPoint source, receiveBuffer, out int length);
                    if (NetSocket.Succeeded(result) == false)
                    {
                        return;
                    }

                    inQueue.Enqueue(source, receiveBuffer, length);
                }
            }
            else
            {
                inQueue.Clear();
            }
        }
#endif
        // #endregion
    }
}
