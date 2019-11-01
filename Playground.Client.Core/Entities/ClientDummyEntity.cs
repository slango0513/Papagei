using Papagei.Client;
using System;

namespace Playground.Client
{
    public class ClientDummyEntity : ClientEntity
    {
        public DummyEntityState State => (DummyEntityState)StateBase;

        public Action Frozen = () => { };
        public Action Unfrozen = () => { };

        public override void Reset()
        {
            base.Reset();
        }
    }
}
