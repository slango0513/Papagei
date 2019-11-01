using Papagei;
using System;
using System.Numerics;

namespace Playground
{
    public class ServerControlledEntity : ServerEntity, IHasPosition
    {
        public MyState State => (MyState)StateBase;

        public Vector2 Position => new Vector2(State.X, State.Y);

        public Action Shutdown = () => { };

        private int actionCount = 0;

        public override void Reset()
        {
            base.Reset();

            actionCount = 0;
        }
    }
}
