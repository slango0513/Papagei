using Papagei.Client;
using System;

namespace Playground.Client
{
    public class ClientMimicEntity : ClientEntity
    {
        public MyState State => (MyState)StateBase;

        public Action Shutdown = () => { };
        public Action Frozen = () => { };
        public Action Unfrozen = () => { };

        public override void Reset()
        {
            base.Reset();
        }
    }
}
