using Papagei;
using System;

namespace Playground
{
    public static class GameMath
    {
        public const float FIXED_DELTA_TIME = 0.02f;

        internal const float COORDINATE_PRECISION = 0.001f;
        internal const float ANGLE_PRECISION = 0.001f;

        internal static bool CoordinatesEqual(float a, float b)
        {
            return Math.Abs(a - b) < COORDINATE_PRECISION;
        }

        internal static bool AnglesEqual(float a, float b)
        {
            return Math.Abs(a - b) < ANGLE_PRECISION;
        }

        internal static float LerpUnclampedFloat(float from, float to, float t)
        {
            return from + ((to - from) * t);
        }
    }

    public static class GameCompressors
    {
        public static readonly SingleCompressor Coordinate = new SingleCompressor(-512.0f, 512.0f, GameMath.COORDINATE_PRECISION / 10.0f);

        public static readonly SingleCompressor Angle = new SingleCompressor(0.0f, 360.0f, GameMath.ANGLE_PRECISION / 10.0f);
    }
}
