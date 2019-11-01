namespace Papagei
{
    public static class BitArrayBitBufferExtensions
    {
        // BitArray8
        public static void WriteBitArray8(this BitBuffer buffer, BitArray8 array)
        {
            buffer.WriteByte(array.BitField);
        }

        public static BitArray8 ReadBitArray8(this BitBuffer buffer)
        {
            return new BitArray8(buffer.ReadByte());
        }

        public static BitArray8 PeekBitArray8(this BitBuffer buffer)
        {
            return new BitArray8(buffer.PeekByte());
        }

        // FixedByteBuffer8
        public static void WriteFixedByteBuffer8(this BitBuffer buffer, FixedByteBuffer8 array)
        {
            var first = FixedByteBuffer8.Pack(array.val0, array.val1, array.val2, array.val3);
            var second = FixedByteBuffer8.Pack(array.val4, array.val5, array.val6, array.val7);
            var writeSecond = second > 0;

            buffer.WriteUInt(first);
            buffer.WriteBool(writeSecond);
            if (writeSecond)
            {
                buffer.WriteUInt(second);
            }
        }

        public static FixedByteBuffer8 ReadFixedByteBuffer8(this BitBuffer buffer)
        {
            uint first = 0;
            uint second = 0;

            first = buffer.ReadUInt();
            if (buffer.ReadBool())
            {
                second = buffer.ReadUInt();
            }

            return new FixedByteBuffer8(first, second);
        }
    }
}
