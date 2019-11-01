using System;
using System.Collections.Generic;
using System.Text;

namespace MiniUDP
{
    internal static class NetEncoding
    {
        internal const int CONNECT_HEADER_SIZE = 3;
        internal const int PROTOCOL_HEADER_SIZE = 3;
        internal const int PAYLOAD_HEADER_SIZE = 3;
        internal const int CARRIER_HEADER_SIZE = 5;
        internal const int NOTIFICATION_HEADER_SIZE = 2;
        internal const int MAX_NOTIFICATION_PACK = NetConfig.DATA_MAXIMUM + NOTIFICATION_HEADER_SIZE;

        /// <summary>
        /// Peeks the type from the packet buffer.
        /// </summary>
        internal static NetPacketType GetType(byte[] buffer)
        {
            return (NetPacketType)buffer[0];
        }

        /// <summary>
        /// Packs a payload to the given buffer.
        /// </summary>
        internal static int PackPayload(byte[] buffer, ushort sequence, byte[] data, ushort dataLength)
        {
            buffer[0] = (byte)NetPacketType.Payload;
            PackU16(buffer, 1, sequence);
            int position = PAYLOAD_HEADER_SIZE;

            Array.Copy(data, 0, buffer, position, dataLength);
            return position + dataLength;
        }

        /// <summary>
        /// Reads payload data from the given buffer.
        /// </summary>
        internal static bool ReadPayload(Func<NetEventType, NetPeer, NetEvent> eventFactory, NetPeer peer, byte[] buffer, int length, out ushort sequence, out NetEvent evnt)
        {
            evnt = null;

            // Read header (already know the type)
            sequence = ReadU16(buffer, 1);
            int position = PAYLOAD_HEADER_SIZE;

            ushort dataLength = (ushort)(length - position);
            if ((position + dataLength) > length)
            {
                return false; // We're reading past the end of the packet data
            }

            evnt = eventFactory.Invoke(NetEventType.Payload, peer);
            return evnt.ReadData(buffer, position, dataLength); ;
        }

        /// <summary>
        /// Packs a series of notifications into the buffer.
        /// </summary>
        internal static int PackCarrier(byte[] buffer, ushort notificationAck, ushort notificationSeq, IEnumerable<NetEvent> notifications)
        {
            int notificationHeaderSize = NOTIFICATION_HEADER_SIZE;

            // Pack header
            buffer[0] = (byte)NetPacketType.Carrier;
            PackU16(buffer, 1, notificationAck);
            PackU16(buffer, 3, notificationSeq);
            int position = CARRIER_HEADER_SIZE;

            // Pack notifications
            int dataPacked = 0;
            int maxDataPack = MAX_NOTIFICATION_PACK;
            foreach (NetEvent notification in notifications)
            {
                // See if we can fit the notification
                int packedSize = notificationHeaderSize + notification.EncodedLength;
                if ((dataPacked + packedSize) > maxDataPack)
                {
                    break;
                }

                // Pack the notification data
                int packSize =
                  PackNotification(
                    buffer,
                    position,
                    notification.EncodedData,
                    notification.EncodedLength);

                // Increment counters
                dataPacked += packSize;
                position += packSize;
            }

            return position;
        }

        /// <summary>
        /// Reads a collection of notifications packed in the buffer.
        /// </summary>
        internal static bool ReadCarrier(Func<NetEventType, NetPeer, NetEvent> eventFactory, NetPeer peer, byte[] buffer, int length, out ushort notificationAck, out ushort notificationSeq, Queue<NetEvent> destinationQueue)
        {
            // Read header (already know the type)
            notificationAck = ReadU16(buffer, 1);
            notificationSeq = ReadU16(buffer, 3);
            int position = CARRIER_HEADER_SIZE;

            // Validate
            int maxDataPack = MAX_NOTIFICATION_PACK;
            if ((position > length) || ((length - position) > maxDataPack))
            {
                return false;
            }

            // Read notifications
            while (position < length)
            {
                NetEvent evnt = eventFactory.Invoke(NetEventType.Notification, peer);
                int bytesRead =
                  ReadNotification(buffer, length, position, evnt);
                if (bytesRead < 0)
                {
                    return false;
                }

                destinationQueue.Enqueue(evnt);
                position += bytesRead;
            }

            return true;
        }

        /// <summary>
        /// Packs a connect request with version and token strings.
        /// </summary>
        internal static int PackConnectRequest(byte[] buffer, string version, string token)
        {
            int versionBytes = Encoding.UTF8.GetByteCount(version);
            int tokenBytes = Encoding.UTF8.GetByteCount(token);

            NetDebug.Assert((byte)versionBytes == versionBytes);
            NetDebug.Assert((byte)tokenBytes == tokenBytes);

            // Pack header info
            buffer[0] = (byte)NetPacketType.Connect;
            buffer[1] = (byte)versionBytes;
            buffer[2] = (byte)tokenBytes;
            int position = CONNECT_HEADER_SIZE;

            Encoding.UTF8.GetBytes(version, 0, version.Length, buffer, position);
            position += versionBytes;
            Encoding.UTF8.GetBytes(token, 0, token.Length, buffer, position);
            position += tokenBytes;

            return position;
        }

        /// <summary>
        /// Reads a packed connect request with version and token strings.
        /// </summary>
        internal static bool ReadConnectRequest(byte[] buffer, out string version, out string token)
        {
            version = "";
            token = "";

            try
            {
                // Already know the type
                byte versionBytes = buffer[1];
                byte tokenBytes = buffer[2];
                int position = CONNECT_HEADER_SIZE;

                version = Encoding.UTF8.GetString(buffer, position, versionBytes);
                position += versionBytes;
                token = Encoding.UTF8.GetString(buffer, position, tokenBytes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Params:
        //    Accept: 0, 0
        //    Disconnect: InternalReason, UserReason
        //    Ping: PingSeq, Loss
        //    Pong: PingSeq, Dropped
        internal static int PackProtocol(byte[] buffer, NetPacketType type, byte firstParam, byte secondParam)
        {
            buffer[0] = (byte)type;
            buffer[1] = firstParam;
            buffer[2] = secondParam;
            return PROTOCOL_HEADER_SIZE;
        }

        internal static bool ReadProtocol(byte[] buffer, int length, out byte firstParam, out byte secondParam)
        {
            // Already know the type
            firstParam = buffer[1];
            secondParam = buffer[2];

            if (length < PROTOCOL_HEADER_SIZE)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Packs a notification prepended with that notification's length.
        /// </summary>
        private static int PackNotification(byte[] buffer, int position, byte[] data, ushort dataLength)
        {
            // For notifications we add the length since there may be multiple
            PackU16(buffer, position, dataLength);
            position += NOTIFICATION_HEADER_SIZE;

            Array.Copy(data, 0, buffer, position, dataLength);
            return NOTIFICATION_HEADER_SIZE + dataLength;
        }

        /// <summary>
        /// Reads a length-prefixed notification block.
        /// </summary>
        private static int ReadNotification(byte[] buffer, int length, int position, NetEvent destination)
        {
            // Read the length we added
            ushort dataLength = ReadU16(buffer, position);
            position += NOTIFICATION_HEADER_SIZE;

            // Avoid a crash if the packet is bad (or malicious)
            if ((position + dataLength) > length)
            {
                return -1;
            }

            // Read the data into the event's buffer
            if (destination.ReadData(buffer, position, dataLength) == false)
            {
                return -1;
            }

            return NOTIFICATION_HEADER_SIZE + dataLength;
        }

        /// <summary>
        /// Encodes a U16 into a buffer at a location in Big Endian order.
        /// </summary>
        private static void PackU16(byte[] buffer, int position, ushort value)
        {
            buffer[position + 0] = (byte)(value >> (8 * 1));
            buffer[position + 1] = (byte)(value >> (8 * 0));
        }

        /// <summary>
        /// Reads a U16 from a buffer at a location in Big Endian order.
        /// </summary>
        private static ushort ReadU16(byte[] buffer, int position)
        {
            int read =
              (buffer[position + 0] << (8 * 1)) |
              (buffer[position + 1] << (8 * 0));
            return (ushort)read;
        }
    }
}
