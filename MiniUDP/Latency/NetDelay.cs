#if DEBUG
using MiniUDP.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace MiniUDP
{
    internal class NetDelay
    {
        private readonly static Noise PingNoise = new Noise();
        private readonly static Noise LossNoise = new Noise();

        private class Entry : IComparable<Entry>
        {
            public long ReleaseTime => releaseTime;
            public IPEndPoint EndPoint => endPoint;
            public byte[] Data => data;

            private readonly long releaseTime;
            private readonly IPEndPoint endPoint;
            private readonly byte[] data;

            public Entry(
              long releaseTime,
              IPEndPoint endPoint,
              byte[] buffer,
              int length)
            {
                this.releaseTime = releaseTime;
                this.endPoint = endPoint;
                data = new byte[length];
                Array.Copy(buffer, 0, data, 0, length);
            }

            public int CompareTo(Entry other)
            {
                return (int)(ReleaseTime - other.releaseTime);
            }
        }

        private class EntryComparer : Comparer<Entry>
        {
            public override int Compare(Entry x, Entry y)
            {
                return (int)(x.ReleaseTime - y.ReleaseTime);
            }
        }

        private readonly Heap<Entry> entries;
        private readonly Stopwatch timer;

        public NetDelay()
        {
            entries = new Heap<Entry>();
            timer = new Stopwatch();
            timer.Start();
        }

        public void Enqueue(IPEndPoint endPoint, byte[] buffer, int length)
        {
            // See if we should drop the packet
            float loss =
              LossNoise.GetValue(
                timer.ElapsedMilliseconds,
                 NetConfig.LossTurbulence);
            if (loss < NetConfig.LossChance)
            {
                return;
            }

            // See if we should delay the packet
            float latencyRange =
        NetConfig.MaximumLatency - NetConfig.MinimumLatency;
            float latencyNoise =
              PingNoise.GetValue(
                timer.ElapsedMilliseconds,
                NetConfig.LatencyTurbulence);
            int latency =
              (int)((latencyNoise * latencyRange) + NetConfig.MinimumLatency);

            long releaseTime = timer.ElapsedMilliseconds + latency;
            entries.Add(new Entry(releaseTime, endPoint, buffer, length));
        }

        public bool TryDequeue(
          out IPEndPoint endPoint,
          out byte[] buffer,
          out int length)
        {
            endPoint = null;
            buffer = null;
            length = 0;

            if (entries.Count > 0)
            {
                Entry first = entries.GetMin();
                if (first.ReleaseTime < timer.ElapsedMilliseconds)
                {
                    entries.ExtractDominating();
                    endPoint = first.EndPoint;
                    buffer = first.Data;
                    length = first.Data.Length;
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            entries.Clear();
        }
    }
}
#endif