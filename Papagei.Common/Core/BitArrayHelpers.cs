using System.Collections.Generic;

namespace Papagei
{
    public static class BitArrayHelpers
    {
        public static IEnumerable<int> GetValues(ulong bits)
        {
            var offset = 0;

            while (bits > 0)
            {
                // Skip 3 bits if possible
                if ((bits & 0x7UL) == 0)
                {
                    bits >>= 3;
                    offset += 3;
                }

                if ((bits & 0x1UL) > 0)
                {
                    yield return offset;
                }

                bits >>= 1;
                offset++;
            }
        }

        public static bool Contains(int value, ulong bits, int length)
        {
            if (value < 0)
            {
                return false;
            }

            if (value >= length)
            {
                return false;
            }

            var bit = 1UL << value;
            return (bits & bit) > 0;
        }
    }
}
