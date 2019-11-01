using MessagePack;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    /// <summary>
    /// A rolling sequence counter for ordering values. Repeats indefinitely
    /// with 1022 possible unique values (0 is treated as invalid internally).
    /// 
    /// Consumes 10 bits when encoded for transmission.
    /// </summary>
    [MessagePackObject]
    public struct SequenceId
    {
        internal class SequenceIdComparer : IEqualityComparer<SequenceId>
        {
            public bool Equals(SequenceId x, SequenceId y)
            {
                return x == y;
            }

            public int GetHashCode(SequenceId x)
            {
                return x.GetHashCode();
            }
        }

        public static readonly IEqualityComparer<SequenceId> Comparer = new SequenceIdComparer();

        public const int BITS_USED = 10; // Max: 1022 unique (0 is invalid)
        private const int MAX_VALUE = (1 << BITS_USED) - 1;
        private const int HALF_WAY_POINT = MAX_VALUE / 2;
        private const int BIT_SHIFT = 32 - BITS_USED;

        public static readonly SequenceId INVALID = new SequenceId(0);
        public static readonly SequenceId START = new SequenceId(1);

        // #region Operators
        private static int GetDifference(SequenceId a, SequenceId b)
        {
            Debug.Assert(a.IsValid);
            Debug.Assert(b.IsValid);

            var difference = (int)((a.RawValue << BIT_SHIFT) - (b.RawValue << BIT_SHIFT));
            return difference;
        }

        private static int WrapValue(int rawValue)
        {
            // We need to skip 0 since it's not a valid number
            if (rawValue > MAX_VALUE)
            {
                return rawValue % MAX_VALUE;
            }
            else if (rawValue < 1)
            {
                return (rawValue % MAX_VALUE) + MAX_VALUE;
            }

            return rawValue;
        }

        public static SequenceId operator +(SequenceId a, int b)
        {
            Debug.Assert(a.IsValid);
            return new SequenceId((uint)WrapValue((int)a.RawValue + b));
        }

        public static SequenceId operator -(SequenceId a, int b)
        {
            Debug.Assert(a.IsValid);
            return new SequenceId((uint)WrapValue((int)a.RawValue - b));
        }

        public static int operator -(SequenceId a, SequenceId b)
        {
            var difference = GetDifference(a, b) >> BIT_SHIFT;

            // We need to skip 0 since it's not a valid number
            if (a.RawValue < b.RawValue)
            {
                if (difference > 0)
                {
                    difference--;
                }
            }
            else
            {
                if (difference < 0)
                {
                    difference++;
                }
            }

            return difference;
        }

        public static SequenceId operator ++(SequenceId a)
        {
            Debug.Assert(a.IsValid);

            return a.Next;
        }

        public static bool operator >(SequenceId a, SequenceId b)
        {
            var difference = GetDifference(a, b);
            return difference > 0;
        }

        public static bool operator <(SequenceId a, SequenceId b)
        {
            var difference = GetDifference(a, b);
            return difference < 0;
        }

        public static bool operator >=(SequenceId a, SequenceId b)
        {
            var difference = GetDifference(a, b);
            return difference >= 0;
        }

        public static bool operator <=(SequenceId a, SequenceId b)
        {
            var difference = GetDifference(a, b);
            return difference <= 0;
        }

        public static bool operator ==(SequenceId a, SequenceId b)
        {
            Debug.Assert(a.IsValid);
            Debug.Assert(b.IsValid);

            return a.RawValue == b.RawValue;
        }

        public static bool operator !=(SequenceId a, SequenceId b)
        {
            Debug.Assert(a.IsValid);
            Debug.Assert(b.IsValid);

            return a.RawValue != b.RawValue;
        }
        // #endregion

        [IgnoreMember]
        public SequenceId Next
        {
            get
            {
                Debug.Assert(IsValid);

                var nextValue = RawValue + 1;
                if (nextValue > MAX_VALUE)
                {
                    nextValue = 1;
                }

                return new SequenceId(nextValue);
            }
        }

        [IgnoreMember]
        public bool IsValid => RawValue > 0;

        [Key(0)]
        public uint RawValue { get; }

        public SequenceId(uint rawValue)
        {
            RawValue = rawValue;
        }

        public override int GetHashCode()
        {
            return (int)RawValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is SequenceId)
            {
                return ((SequenceId)obj).RawValue == RawValue;
            }

            return false;
        }

        public override string ToString()
        {
            if (IsValid)
            {
                return "SequenceId:" + (RawValue - 1);
            }

            return "SequenceId:INVALID";
        }
    }
}
