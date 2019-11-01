using System;

namespace MiniUDP
{
    /// <summary>
    /// Module for traffic management and connection quality assessment.
    /// </summary>
    public class NetTraffic
    {
        internal const int LOSS_BITS = 224;
        internal const int PING_HISTORY = 64;

        /// <summary>
        /// Sliding bit array keeping a history of received sequence numbers.
        /// </summary>
        internal class SequenceCounter
        {
            private readonly int numChunks;
            internal readonly uint[] data;

            private ushort latestSequence;

            public SequenceCounter(bool startFilled = true)
            {
                numChunks = LOSS_BITS / 32;
                data = new uint[numChunks];
                latestSequence = 0;

                if (startFilled)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = 0xFFFFFFFF;
                    }
                }
            }

            public int ComputeCount()
            {
                uint sum = 0;
                for (int i = 0; i < numChunks; i++)
                {
                    sum += HammingWeight(data[i]);
                }

                return (int)sum;
            }

            /// <summary>
            /// Logs the sequence in the accumulator.
            /// </summary>
            public void Store(ushort sequence)
            {
                int difference =
                  NetUtil.UShortSeqDiff(latestSequence, sequence);

                if (difference == 0)
                {
                    return;
                }

                if (difference >= LOSS_BITS)
                {
                    return;
                }

                if (difference > 0)
                {
                    SetBit(difference);
                    return;
                }

                Shift(-difference);
                latestSequence = sequence;
                data[0] |= 1;
            }

            /// <summary>
            /// Advances to a given sequence without storing anything.
            /// </summary>
            public void Advance(ushort sequence)
            {
                int difference =
                  NetUtil.UShortSeqDiff(latestSequence, sequence);
                if (difference < 0)
                {
                    Shift(-difference);
                    latestSequence = sequence;
                }
            }

            /// <summary>
            /// Shifts the entire array by a given number of bits.
            /// </summary>
            private void Shift(int count)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException("count");
                }

                int chunks = count / 32;
                int bits = count % 32;

                int i = numChunks - 1;
                int min = chunks;

                for (; i >= min; i--)
                {
                    int sourceChunk = i - chunks;
                    int sourceNext = i - (chunks + 1);

                    ulong dataHigh = data[sourceChunk];
                    ulong dataLow =
                      (sourceNext >= 0) ? data[sourceNext] : 0;
                    data[i] =
                      (uint)((((dataHigh << 32) | dataLow) << bits) >> 32);
                }

                for (; i >= 0; i--)
                {
                    data[i] = 0;
                }
            }

            /// <summary>
            /// Returns true iff the value is already contained.
            /// </summary>
            private bool SetBit(int index)
            {
                if ((index < 0) || (index >= LOSS_BITS))
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                int chunkIdx = index / 32;
                int chunkBit = index % 32;

                uint bit = 1U << chunkBit;
                uint chunk = data[chunkIdx];

                if ((bit & chunk) != 0)
                {
                    return true;
                }

                chunk |= bit;
                data[chunkIdx] = chunk;
                return false;
            }

            private uint HammingWeight(uint chunk)
            {
                chunk = chunk - ((chunk >> 1) & 0x55555555);
                chunk = (chunk & 0x33333333) + ((chunk >> 2) & 0x33333333);
                return (((chunk + (chunk >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
            }
        }

        /// <summary>
        /// A sliding history window of outgoing ping information and support
        /// for cross-referencing incoming pongs against that history.
        /// </summary>
        internal class PingCounter
        {
            private readonly long[] pingTimes;
            private readonly byte[] pingSequences;
            private byte currentPingSeq;

            public PingCounter()
            {
                pingTimes = new long[PING_HISTORY];
                pingSequences = new byte[PING_HISTORY];
                for (int i = 0; i < pingTimes.Length; i++)
                {
                    pingTimes[i] = -1;
                }
            }

            /// <summary>
            /// Creates a new outgoing ping. Stores both that ping's sequence
            /// and the time it was created.
            /// </summary>
            public byte CreatePing(long curTime)
            {
                currentPingSeq++;
                int index = currentPingSeq % PING_HISTORY;
                pingTimes[index] = curTime;
                pingSequences[index] = currentPingSeq;
                return currentPingSeq;
            }

            /// <summary>
            /// Returns the time the ping was created for the given pong.
            /// Checks to make sure the stored slot corresponds to the sequence.
            /// </summary>
            public long ConsumePong(byte pongSeq)
            {
                int index = pongSeq % PING_HISTORY;
                if (pingSequences[index] != pongSeq)
                {
                    return -1;
                }

                long pingTime = pingTimes[index];
                pingTimes[index] = -1;
                return pingTime;
            }
        }

        /// <summary>
        /// Computes the average ping over a window.
        /// </summary>
        private static float PingAverage(int[] window)
        {
            float sum = 0.0f;
            int count = 0;
            for (int i = 0; i < window.Length; i++)
            {
                if (window[i] >= 0)
                {
                    sum += window[i];
                    count++;
                }
            }

            if (count > 0)
            {
                return sum / count;
            }

            return -1.0f;
        }

        // May be accessed from main thread
        public float Ping { get; private set; }
        public float LocalLoss { get; private set; }
        public float RemoteLoss { get; private set; }
        public float LocalDrop { get; private set; }
        public float RemoteDrop { get; private set; }
        public long TimeSinceCreation { get; private set; }
        public long TimeSinceReceive { get; private set; }
        public long TimeSincePayload { get; private set; }
        public long TimeSinceNotification { get; private set; }
        public long TimeSincePong { get; private set; }

        internal ushort NotificationAck => notificationAck;

        private readonly SequenceCounter payloadLoss;
        private readonly SequenceCounter payloadDrop;
        private readonly PingCounter outgoingPing;
        private readonly int[] pingWindow;
        private readonly long creationTime;

        private ushort lastPayloadSeq;
        private ushort notificationAck;
        private int pingWindowIndex;

        private long lastPacketRecvTime; // Time we last received anything
        private long lastPayloadRecvTime;
        private long lastNotificationRecvTime;
        private long lastPongRecvTime;

        internal NetTraffic(long creationTime)
        {
            payloadLoss = new SequenceCounter(true);
            payloadDrop = new SequenceCounter(false);
            outgoingPing = new PingCounter();
            pingWindow = new int[NetConfig.PING_SMOOTHING_WINDOW];
            this.creationTime = creationTime;

            lastPayloadSeq = ushort.MaxValue; // "-1"
            notificationAck = 0;
            pingWindowIndex = 0;

            lastPacketRecvTime = creationTime;
            lastPayloadRecvTime = creationTime;
            lastNotificationRecvTime = creationTime;
            lastPongRecvTime = creationTime;

            for (int i = 0; i < pingWindow.Length; i++)
            {
                pingWindow[i] = -1;
            }
        }

        internal void Update(long curTime)
        {
            TimeSinceCreation = curTime - creationTime;
            TimeSinceReceive = curTime - lastPacketRecvTime;
            TimeSincePayload = curTime - lastPayloadRecvTime;
            TimeSinceNotification = curTime - lastNotificationRecvTime;
            TimeSincePong = curTime - lastPongRecvTime;

            if (TimeSincePong > (pingWindow.Length * 1000))
            {
                for (int i = 0; i < pingWindow.Length; i++)
                {
                    pingWindow[i] = -1;
                }

                Ping = -1.0f;
            }
        }

        internal long GetTimeSinceRecv(long curTime)
        {
            return curTime - lastPacketRecvTime;
        }

        internal byte GeneratePing(long curTime)
        {
            return outgoingPing.CreatePing(curTime);
        }

        internal byte GenerateLoss()
        {
            int count = payloadLoss.ComputeCount();
            int missing = LOSS_BITS - count;
            return (byte)missing;
        }

        internal byte GenerateDrop()
        {
            return (byte)payloadDrop.ComputeCount();
        }

        /// <summary>
        /// Processes the loss value from a received ping.
        /// </summary>
        internal void OnReceivePing(long curTime, byte loss)
        {
            lastPacketRecvTime = curTime;

            // Recompute since it may be read on the main thread
            RemoteLoss = loss / (float)LOSS_BITS;
        }

        /// <summary>
        /// Receives a pong and updates connection timings.
        /// </summary>
        internal void OnReceivePong(long curTime, byte pongSeq, byte drop)
        {
            // Reject it if it's too old, including statistics for it
            long creationTime = outgoingPing.ConsumePong(pongSeq);
            if (creationTime < 0)
            {
                return;
            }

            long diff = curTime - creationTime;
            if (diff < 0)
            {
                return;
            }

            lastPacketRecvTime = curTime;
            lastPongRecvTime = curTime;

            pingWindow[pingWindowIndex] = (int)diff;
            pingWindowIndex =
              (pingWindowIndex + 1) % pingWindow.Length;

            // Recompute since it may be read on the main thread
            Ping = PingAverage(pingWindow);
            RemoteDrop = drop / (float)LOSS_BITS;
        }

        /// <summary>
        /// Logs the receipt of a payload for packet loss calculation.
        /// Returns false iff the payload is too old and should be rejected.
        /// </summary>
        internal bool OnReceivePayload(long curTime, ushort payloadSeq)
        {
            bool isNew = IsPayloadNew(payloadSeq);
            payloadLoss.Store(payloadSeq);

            if (isNew)
            {
                lastPacketRecvTime = curTime;
                lastPayloadRecvTime = curTime;
                lastPayloadSeq = payloadSeq;
                payloadDrop.Advance(payloadSeq);
            }
            else
            {
                payloadDrop.Store(payloadSeq);
            }

            // Recompute since it may be read on the main thread
            LocalLoss = GenerateLoss() / (float)LOSS_BITS;
            LocalDrop = GenerateDrop() / (float)LOSS_BITS;
            return isNew;
        }

        /// <summary>
        /// Logs the receipt of a notification for timing and keepalive.
        /// Returns false iff the notification is too old and should be rejected.
        /// </summary>
        internal bool OnReceiveNotification(long curTime, ushort notificationSeq)
        {
            // Reject it if it's too old, including statistics for it
            if (NetUtil.UShortSeqDiff(notificationSeq, NotificationAck) <= 0)
            {
                return false;
            }

            notificationAck = notificationSeq;
            lastPacketRecvTime = curTime;
            lastNotificationRecvTime = curTime;
            return true;
        }

        /// <summary>
        /// For all other packet types.
        /// </summary>
        internal void OnReceiveOther(long curTime)
        {
            lastPacketRecvTime = curTime;
        }

        /// <summary>
        /// Returns true iff a payload sequence is new.
        /// </summary>
        private bool IsPayloadNew(ushort sequence)
        {
            int difference =
              NetUtil.UShortSeqDiff(lastPayloadSeq, sequence);
            return difference < 0;
        }
    }
}
