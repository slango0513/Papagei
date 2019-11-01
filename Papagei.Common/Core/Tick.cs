using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public class TickComparer : Comparer<Tick>, IEqualityComparer<Tick>
    {
        private readonly Comparer<uint> _comparer = Comparer<uint>.Default;

        public override int Compare(Tick x, Tick y)
        {
            Debug.Assert(x.IsValid);
            Debug.Assert(y.IsValid);
            return _comparer.Compare(x.TickValue, y.TickValue);
        }

        public bool Equals(Tick x, Tick y)
        {
            Debug.Assert(x.IsValid);
            Debug.Assert(y.IsValid);
            return x == y;
        }

        public int GetHashCode(Tick x)
        {
            Debug.Assert(x.IsValid);
            return x.GetHashCode();
        }
    }

    /// <summary>
    /// A type-safe and zero-safe wrapper for a tick int. Supports basic
    /// operations and encoding. All internal values are offset by +1 (zero
    /// is invalid, 1 is tick zero, etc.).
    /// </summary>
    [MessagePackObject]
    public struct Tick
    {
        public static Tick Subtract(Tick a, int b, bool warnClamp = false)
        {
            Debug.Assert(b >= 0);
            var result = a.TickValue - b;
            if (result < 1)
            {
                if (warnClamp)
                {
                    Console.WriteLine("Clamping tick subtraction");
                }

                result = 1;
            }
            return new Tick((uint)result);
        }

        public static readonly Tick INVALID = new Tick(0);
        public static readonly Tick START = new Tick(1);

        // #region Operators
        // Can't find references on these, so just delete and build to find uses

        public static Tick operator ++(Tick a)
        {
            return a.GetNext();
        }

        public static bool operator ==(Tick a, Tick b)
        {
            return a.TickValue == b.TickValue;
        }

        public static bool operator !=(Tick a, Tick b)
        {
            return a.TickValue != b.TickValue;
        }

        public static bool operator <(Tick a, Tick b)
        {
            Debug.Assert(a.IsValid && b.IsValid);
            return a.TickValue < b.TickValue;
        }

        public static bool operator <=(Tick a, Tick b)
        {
            Debug.Assert(a.IsValid && b.IsValid);
            return a.TickValue <= b.TickValue;
        }

        public static bool operator >(Tick a, Tick b)
        {
            Debug.Assert(a.IsValid && b.IsValid);
            return a.TickValue > b.TickValue;
        }

        public static bool operator >=(Tick a, Tick b)
        {
            Debug.Assert(a.IsValid && b.IsValid);
            return a.TickValue >= b.TickValue;
        }

        public static int operator -(Tick a, Tick b)
        {
            Debug.Assert(a.IsValid && b.IsValid);
            long difference = a.TickValue - (long)b.TickValue;
            return (int)difference;
        }

        public static Tick operator +(Tick a, uint b)
        {
            Debug.Assert(a.IsValid);
            return new Tick(a.TickValue + b);
        }

        public static Tick operator -(Tick a, int b)
        {
            return Subtract(a, b, true);
        }
        // #endregion

        // #region Properties
        [IgnoreMember]
        public bool IsValid => TickValue > 0;

        public float ToTime(float tickDeltaTime)
        {
            Debug.Assert(IsValid);
            return (TickValue - 1) * tickDeltaTime;
        }
        // #endregion

        /// <summary>
        /// Should be used very sparingly. Otherwise it defeats type safety.
        /// </summary>
        [IgnoreMember]
        public uint RawValue
        {
            get
            {
                Debug.Assert(IsValid);
                return TickValue - 1;
            }
        }

        [Key(0)]
        public uint TickValue { get; }

        public Tick(uint tickValue)
        {
            TickValue = tickValue;
        }

        public Tick GetNext()
        {
            Debug.Assert(IsValid);
            return new Tick(TickValue + 1);
        }

        public override int GetHashCode()
        {
            return (int)TickValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is Tick)
            {
                return ((Tick)obj).TickValue == TickValue;
            }

            return false;
        }

        public override string ToString()
        {
            if (TickValue == 0)
            {
                return "Tick:INVALID";
            }

            return "Tick:" + (TickValue - 1);
        }
    }
}
