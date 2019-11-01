using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public struct BitArray32
    {
        public const int LENGTH = 32;

        public uint Bits { get; }

        public static BitArray32 operator <<(BitArray32 a, int b)
        {
            return new BitArray32(a.Bits << b);
        }

        public static BitArray32 operator >>(BitArray32 a, int b)
        {
            return new BitArray32(a.Bits >> b);
        }

        private BitArray32(uint bitField)
        {
            Bits = bitField;
        }

        public BitArray32 Store(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray32(Bits | (1U << value));
        }

        public BitArray32 Remove(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray32(Bits & ~(1U << value));
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
