using System;

namespace Papagei
{
    /// <summary>
    /// Compresses floats to a given range with a given precision.
    /// http://stackoverflow.com/questions/8382629/compress-floating-point-numbers-with-specified-range-and-precision
    /// </summary>
    public class SingleCompressor
    {
        private readonly float precision;
        private readonly float invPrecision;

        private readonly float minValue;
        private readonly float maxValue;
        private readonly uint mask;

        public int RequiredBits { get; }

        public SingleCompressor(float minValue, float maxValue, float precision)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.precision = precision;

            invPrecision = 1.0f / precision;
            RequiredBits = ComputeRequiredBits();
            mask = (uint)((1L << RequiredBits) - 1);
        }

        public uint Pack(float value)
        {
            var newValue = Util.Clamp(value, minValue, maxValue);
            if (newValue != value)
            {
                Console.WriteLine($"Clamping value for send! {value} vs. [{minValue},{maxValue}]");
            }

            var adjusted = (value - minValue) * invPrecision;
            return (uint)(adjusted + 0.5f) & mask;
        }

        public float Unpack(uint data)
        {
            var adjusted = (data * precision) + minValue;
            return Util.Clamp(adjusted, minValue, maxValue);
        }

        private int ComputeRequiredBits()
        {
            var range = maxValue - minValue;
            var maxVal = range * (1.0f / precision);
            return Util.Log2((uint)(maxVal + 0.5f)) + 1;
        }
    }
}
