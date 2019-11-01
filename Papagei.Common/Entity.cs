namespace Papagei
{
    /// <summary>
    /// Entities represent any object existent in the world. These can be 
    /// "physical" objects that move around and do things like pawns and
    /// vehicles, or conceptual objects like scoreboards and teams that 
    /// mainly serve as blackboards for transmitting state data.
    /// 
    /// In order to register an Entity class, tag it with the
    /// [RegisterEntity] attribute. See Registry.cs for more information.
    /// </summary>
    public abstract class Entity
    {
        protected void ResetCore()
        {
            Id = EntityId.INVALID;
            HasStarted = false;

            // We always notify a controller change at start
            DeferNotifyControllerChanged = true;

            {
                //ResetStates();
                if (StateBase != null)
                {
                    StateBase.Pool.Deallocate(StateBase);
                }

                StateBase = null;
            }
        }

        protected internal virtual UpdateOrder UpdateOrder => UpdateOrder.Normal;

        // Simulation info
        public State StateBase { get; set; }

        // Synchronization info
        public EntityId Id { get; set; }
        public Tick RemovedTick { get; set; }

        //private int factoryType;
        public bool HasStarted { get; set; }
        public bool DeferNotifyControllerChanged { get; set; }
    }
}
