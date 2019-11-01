using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public struct BitArray8
    {
        private const int LENGTH = 8;
        public static readonly BitArray8 EMPTY = new BitArray8(0);

        public byte BitField => _bitField;
        private readonly byte _bitField;

        public static BitArray8 operator <<(BitArray8 a, int b)
        {
            return new BitArray8((byte)(a._bitField << b));
        }

        public static BitArray8 operator >>(BitArray8 a, int b)
        {
            return new BitArray8((byte)(a._bitField >> b));
        }

        public static bool operator ==(BitArray8 a, BitArray8 b)
        {
            return a._bitField == b._bitField;
        }

        public static bool operator !=(BitArray8 a, BitArray8 b)
        {
            return a._bitField != b._bitField;
        }

        public BitArray8(byte bitField)
        {
            _bitField = bitField;
        }

        public BitArray8 Store(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray8((byte)(_bitField | (1U << value)));
        }

        public BitArray8 Remove(int value)
        {
            Debug.Assert(value < LENGTH);
            return new BitArray8((byte)(_bitField & ~(1U << value)));
        }

        public IEnumerable<int> GetValues()
        {
            return BitArrayHelpers.GetValues(_bitField);
        }

        public bool Contains(int value)
        {
            return BitArrayHelpers.Contains(value, _bitField, LENGTH);
        }

        public bool IsEmpty()
        {
            return _bitField == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is BitArray8)
            {
                return ((BitArray8)obj)._bitField == _bitField;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _bitField;
        }
    }
}
