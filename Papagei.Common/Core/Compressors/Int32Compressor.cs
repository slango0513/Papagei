using System;

namespace Papagei
{
    public class Int32Compressor
    {
        private readonly int minValue;
        private readonly int maxValue;
        private readonly uint mask;

        public int RequiredBits { get; }

        public Int32Compressor(int minValue, int maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;

            RequiredBits = ComputeRequiredBits();
            mask = (uint)((1L << RequiredBits) - 1);
        }

        public uint Pack(int value)
        {
            if ((value < minValue) || (value > maxValue))
            {
                Console.WriteLine($"Clamping value for send! {value} vs. [{minValue},{maxValue}]");
            }

            return (uint)(value - minValue) & mask;
        }

        public int Unpack(uint data)
        {
            return (int)(data + minValue);
        }

        private int ComputeRequiredBits()
        {
            if (minValue >= maxValue)
            {
                return 0;
            }

            var minLong = minValue;
            var maxLong = maxValue;
            var range = (uint)(maxLong - minLong);
            return Util.Log2(range) + 1;
        }
    }
}
