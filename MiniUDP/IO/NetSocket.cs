using System.Net;
using System.Net.Sockets;

namespace MiniUDP
{
    /// <summary>
    /// Since raw sockets are thread safe, we use a global socket singleton
    /// between the two threads for the sake of convenience.
    /// </summary>
    internal class NetSocket
    {
        public static bool Succeeded(SocketError error)
        {
            return error == SocketError.Success;
        }

        public static bool Empty(SocketError error)
        {
            return error == SocketError.NoData;
        }

        // https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.aspx
        // We don't need a lock for writing, but we do for reading because polling
        // and receiving are two different non-atomic actions. In practice we
        // should only ever be reading from the socket on one thread anyway.
        private readonly object readLock = new object();
        private readonly Socket rawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveBufferSize = NetConfig.SOCKET_BUFFER_SIZE,
            SendBufferSize = NetConfig.SOCKET_BUFFER_SIZE,
            Blocking = false
        };

        internal NetSocket()
        {
            try
            {
                // Ignore port unreachable (connection reset by remote host)
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                var SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                rawSocket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch
            {
                // Not always supported
                NetDebug.LogWarning("Failed to set control code for ignoring ICMP port unreachable.");
            }
        }

        internal SocketError Bind(int port)
        {
            try
            {
                rawSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException exception)
            {
                return exception.SocketErrorCode;
            }
            return SocketError.Success;
        }

        internal void Close()
        {
            rawSocket.Close();
        }

        /// <summary> 
        /// Attempts to send data to endpoint via OS socket. 
        /// Returns false if the send failed.
        /// </summary>
        internal SocketError TrySend(IPEndPoint destination, byte[] buffer, int length)
        {
            try
            {
                var bytesSent = rawSocket.SendTo(buffer, length, SocketFlags.None, destination);
                if (bytesSent == length)
                {
                    return SocketError.Success;
                }

                return SocketError.MessageSize;
            }
            catch (SocketException exception)
            {
                NetDebug.LogError($"Send failed: {exception.Message}");
                NetDebug.LogError(exception.StackTrace);
                return exception.SocketErrorCode;
            }
        }

        /// <summary> 
        /// Attempts to read from OS socket. Returns false if the read fails
        /// or if there is nothing to read.
        /// </summary>
        internal SocketError TryReceive(out IPEndPoint source, byte[] destBuffer, out int length)
        {
            source = null;
            length = 0;

            lock (readLock)
            {
                if (rawSocket.Poll(0, SelectMode.SelectRead) == false)
                {
                    return SocketError.NoData;
                }

                try
                {
                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

                    length = rawSocket.ReceiveFrom(destBuffer, destBuffer.Length, SocketFlags.None, ref endPoint);

                    if (length > 0)
                    {
                        source = endPoint as IPEndPoint;
                        return SocketError.Success;
                    }

                    return SocketError.NoData;
                }
                catch (SocketException exception)
                {
                    NetDebug.LogError("Receive failed: " + exception.Message);
                    NetDebug.LogError(exception.StackTrace);
                    return exception.SocketErrorCode;
                }
            }
        }
    }
}
