using Papagei;
using System;
using System.Numerics;

namespace Playground
{
    public class ServerDummyEntity : ServerEntity, IHasPosition
    {
        public DummyEntityState State => (DummyEntityState)StateBase;

        public Random random = new Random();

        public Action Frozen = () => { };
        public Action Unfrozen = () => { };

        public Vector2 Position => new Vector2(State.X, State.Y);

        public float startX;
        public float startY;
        public float startZ;
        public float distance;
        public float angle;
        public float speed;

        public override void Reset()
        {
            base.Reset();

            startX = 0.0f;
            startY = 0.0f;
            startZ = 0.0f;
            distance = 0.0f;
            angle = 0.0f;
            speed = 0.0f;
        }
    }
}
