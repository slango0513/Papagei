using Papagei;
using System;

namespace Playground
{
    public class DummyEntityState : State
    {
        [Flags]
        public enum Props : uint
        {
            // 0x0
            None = 0,
            // 0x1
            X = 1 << 0,
            // 0x2
            Y = 1 << 1,
            // 0x4
            Z = 1 << 2,
            // 0x8
            Angle = 1 << 3,
            // 0x16
            Status = 1 << 4,

            All = X | Y | Z | Angle | Status,
        }

        public override int FlagBits => 5;

        // These should be properties, but we can't pass properties by ref
        public int ArchetypeId;
        public int UserId;

        public float X;
        public float Y;
        public float Z;
        public float Angle;
        public byte Status;

        public override void Reset()
        {
            base.Reset();

            ArchetypeId = 0;
            UserId = 0;
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
            Angle = 0.0f;
            Status = 0;
        }

        public override void ApplyMutableFrom(State source, uint flags)
        {
            var _other = (DummyEntityState)source;
            var _flags = (Props)flags;
            if (_flags.HasFlag(Props.X))
            {
                X = _other.X;
            }
            if (_flags.HasFlag(Props.Y))
            {
                Y = _other.Y;
            }
            if (_flags.HasFlag(Props.Z))
            {
                Z = _other.Z;
            }
            if (_flags.HasFlag(Props.Angle))
            {
                Angle = _other.Angle;
            }
            if (_flags.HasFlag(Props.Status))
            {
                Status = _other.Status;
            }
        }

        public override void ApplyControllerFrom(State source) { }

        public override void ApplyImmutableFrom(State source)
        {
            var _other = (DummyEntityState)source;
            ArchetypeId = _other.ArchetypeId;
            UserId = _other.UserId;
        }

        public override void ResetControllerData() { }

        public override uint CompareMutableData(State basis)
        {
            var _basis = (DummyEntityState)basis;
            var flags = (!GameMath.CoordinatesEqual(X, _basis.X) ? Props.X : 0)
                | (!GameMath.CoordinatesEqual(Y, _basis.Y) ? Props.Y : 0)
                | (!GameMath.CoordinatesEqual(Z, _basis.Z) ? Props.Z : 0)
                | (!GameMath.AnglesEqual(Angle, _basis.Angle) ? Props.Angle : 0)
                | (Status != _basis.Status ? Props.Status : 0);
            return (uint)flags;
        }

        public override bool IsControllerDataEqual(State basis)
        {
            return true;
        }

        public override void ApplyInterpolated(State first, State second, float t)
        {
            var _first = (DummyEntityState)first;
            var _second = (DummyEntityState)second;
            X = GameMath.LerpUnclampedFloat(_first.X, _second.X, t);
            Y = GameMath.LerpUnclampedFloat(_first.Y, _second.Y, t);
            Z = GameMath.LerpUnclampedFloat(_first.Z, _second.Z, t);
        }
    }
}
