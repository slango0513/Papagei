using Papagei.Client;
using System;

namespace Playground.Client
{
    public class ClientControlledEntity : ClientEntity
    {
        public MyState State => (MyState)StateBase;

        public Action Shutdown = () => { };
        public Action Frozen = () => { };
        public Action Unfrozen = () => { };

        private int actionCount = 0;

        public override void Reset()
        {
            base.Reset();

            actionCount = 0;
        }

        public bool Up, Down, Left, Right, Action;
    }
}
