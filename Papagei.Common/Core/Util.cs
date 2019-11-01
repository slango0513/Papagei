namespace Papagei
{
    public static class Util
    {
        // http://stackoverflow.com/questions/15967240/fastest-implementation-of-log2int-and-log2float
        private static readonly int[] DeBruijnLookup = new int[32]
        {
            0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
            8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
        };

        private static readonly object _lockObj = new object();

        public static int Log2(uint v)
        {
            v |= v >> 1; // Round down to one less than a power of 2 
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;

            lock (_lockObj)
            {
                return DeBruijnLookup[(v * 0x07C4ACDDU) >> 27];
            }
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = b;
            b = a;
            a = temp;
        }

        public static bool GetFlag(byte field, byte flag)
        {
            return (field & flag) > 0;
        }

        public static byte SetFlag(byte field, byte flag, bool value)
        {
            if (value)
            {
                return (byte)(field | flag);
            }

            return (byte)(field & ~flag);
        }

        public static int Abs(int a)
        {
            if (a < 0)
            {
                return -a;
            }

            return a;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
            return value;
        }
    }
}
