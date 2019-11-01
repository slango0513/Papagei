using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public struct BitArray16
    {
        public const int LENGTH = 16;

        public ushort Bits { get; }

        public static BitArray16 operator <<(BitArray16 a, int b)
        {
            return new BitArray16((ushort)(a.Bits << b));
        }

        public static BitArray16 operator >>(BitArray16 a, int b)
        {
            return new BitArray16((ushort)(a.Bits >> b));
        }

        private BitArray16(ushort bitField)
        {
            Bits = bitField;
        }

        public BitArray16 Store(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray16((ushort)(Bits | (1U << value)));
        }

        public BitArray16 Remove(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray16((ushort)(Bits & ~(1U << value)));
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
