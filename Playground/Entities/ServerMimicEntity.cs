using Papagei;
using System;
using System.Numerics;

namespace Playground
{
    public class ServerMimicEntity : ServerEntity, IHasPosition
    {
        public MyState State => (MyState)StateBase;

        public Action Shutdown = () => { };

        public Vector2 Position => new Vector2(State.X, State.Y);

        public ServerControlledEntity controlled;
        public float xOffset;
        public float yOffset;

        public override void Reset()
        {
            base.Reset();

            controlled = null;
            xOffset = 0.0f;
            yOffset = 0.0f;
        }
    }
}
