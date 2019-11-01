using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public struct BitArray64
    {
        public const int LENGTH = 64;

        public ulong Bits { get; }

        public static BitArray64 operator <<(BitArray64 a, int b)
        {
            return new BitArray64(a.Bits << b);
        }

        public static BitArray64 operator >>(BitArray64 a, int b)
        {
            return new BitArray64(a.Bits >> b);
        }

        private BitArray64(ulong bitField)
        {
            Bits = bitField;
        }

        public BitArray64 Store(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray64(Bits | (1UL << value));
        }

        public BitArray64 Remove(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray64(Bits & ~(1UL << value));
        }

        public IEnumerable<int> GetValues()
        {
            return BitArrayHelpers.GetValues(Bits);
        }

        public bool Contains(int value)
        {
            return BitArrayHelpers.Contains(value, Bits, LENGTH);
        }
    }
}
