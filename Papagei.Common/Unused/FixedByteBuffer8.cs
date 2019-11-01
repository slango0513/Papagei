using System;

namespace Papagei
{
    public struct FixedByteBuffer8
    {
        [ThreadStatic]
        private static byte[] BYTE_BUFFER = null;

        public static uint Pack(byte a, byte b, byte c, byte d)
        {
            return ((uint)a << 0) | ((uint)b << 8) | ((uint)c << 16) | ((uint)d << 24);
        }

        private static void Unpack(uint value, out byte a, out byte b, out byte c, out byte d)
        {
            a = (byte)(value >> 0);
            b = (byte)(value >> 8);
            c = (byte)(value >> 16);
            d = (byte)(value >> 24);
        }

        public readonly byte val0;
        public readonly byte val1;
        public readonly byte val2;
        public readonly byte val3;
        public readonly byte val4;
        public readonly byte val5;
        public readonly byte val6;
        public readonly byte val7;

        public FixedByteBuffer8(byte val0 = 0, byte val1 = 0, byte val2 = 0, byte val3 = 0, byte val4 = 0, byte val5 = 0, byte val6 = 0, byte val7 = 0)
        {
            this.val0 = val0;
            this.val1 = val1;
            this.val2 = val2;
            this.val3 = val3;
            this.val4 = val4;
            this.val5 = val5;
            this.val6 = val6;
            this.val7 = val7;
        }

        public FixedByteBuffer8(uint first, uint second)
        {
            Unpack(first, out val0, out val1, out val2, out val3);
            Unpack(second, out val4, out val5, out val6, out val7);
        }

        public FixedByteBuffer8(byte[] buffer, int count)
        {
            if (count < buffer.Length)
            {
                throw new ArgumentException("count < buffer.Length");
            }

            val0 = 0;
            val1 = 0;
            val2 = 0;
            val3 = 0;
            val4 = 0;
            val5 = 0;
            val6 = 0;
            val7 = 0;

            if (count > 0)
            {
                val0 = buffer[0];
            }

            if (count > 1)
            {
                val1 = buffer[1];
            }

            if (count > 2)
            {
                val2 = buffer[2];
            }

            if (count > 3)
            {
                val3 = buffer[3];
            }

            if (count > 4)
            {
                val4 = buffer[4];
            }

            if (count > 5)
            {
                val5 = buffer[5];
            }

            if (count > 6)
            {
                val6 = buffer[6];
            }

            if (count > 7)
            {
                val7 = buffer[7];
            }
        }

        public void Output(byte[] buffer)
        {
            buffer[0] = val0;
            buffer[1] = val1;
            buffer[2] = val2;
            buffer[3] = val3;
            buffer[4] = val4;
            buffer[5] = val5;
            buffer[6] = val6;
            buffer[7] = val7;
        }

        /// <summary>
        /// Outputs to a pre-allocated per-thread byte buffer. Note that this
        /// buffer is reused between calls and may be overwritten.
        /// </summary>
        public byte[] OutputBuffered()
        {
            if (BYTE_BUFFER == null)
            {
                BYTE_BUFFER = new byte[8];
            }

            Output(BYTE_BUFFER);
            return BYTE_BUFFER;
        }
    }
}
