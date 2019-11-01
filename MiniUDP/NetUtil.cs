using System;
using System.Net;

namespace MiniUDP
{
    public static class NetUtil
    {
        /// <summary>
        /// Validates that a given kick reason is acceptable for a remote kick.
        /// </summary>
        internal static NetCloseReason ValidateKickReason(NetCloseReason reason)
        {
            switch (reason)
            {
                case NetCloseReason.RejectNotHost:
                    return reason;
                case NetCloseReason.RejectFull:
                    return reason;
                case NetCloseReason.RejectVersion:
                    return reason;
                case NetCloseReason.KickTimeout:
                    return reason;
                case NetCloseReason.KickShutdown:
                    return reason;
                case NetCloseReason.KickError:
                    return reason;
                case NetCloseReason.KickUserReason:
                    return reason;
            }

            NetDebug.LogError("Bad kick reason: " + reason);
            return NetCloseReason.INVALID;
        }

        /// <summary>
        /// Compares two bytes a - b with wrap-around arithmetic.
        /// </summary>
        internal static int ByteSeqDiff(byte a, byte b)
        {
            // Assumes a sequence is 8 bits
            return ((a << 24) - (b << 24)) >> 24;
        }

        /// <summary>
        /// Compares two ushorts a - b with wrap-around arithmetic.
        /// </summary>
        internal static int UShortSeqDiff(ushort a, ushort b)
        {
            // Assumes a stamp is 16 bits
            return ((a << 16) - (b << 16)) >> 16;
        }

        /// <summary>
        /// Returns an IPv4 IP:Port string as an IPEndpoint.
        /// </summary>
        public static IPEndPoint StringToEndPoint(string address)
        {
            string[] split = address.Split(':');
            string stringAddress = split[0];
            string stringPort = split[1];

            int port = int.Parse(stringPort);
            IPAddress ipaddress = IPAddress.Parse(stringAddress);
            IPEndPoint endpoint = new IPEndPoint(ipaddress, port);

            if (endpoint == null)
            {
                throw new ArgumentException("Failed to parse address: " + address);
            }

            return endpoint;
        }
    }
}
