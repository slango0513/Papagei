namespace Papagei
{
    /// <summary>
    /// Commands contain input data from the client to be applied to entities.
    /// </summary>
    public abstract class Command : IPoolable<Command>, ITimedValue
    {
        // #region Pooling
        public IPool<Command> Pool { get; set; }

        public virtual void Reset()
        {
            ClientTick = Tick.INVALID;
        }
        // #endregion

        // #region Interface
        public Tick Tick => ClientTick;
        // #endregion

        public abstract void SetDataFrom(Command other);

        /// <summary>
        /// The client's local tick (not server predicted) at the time of sending.
        /// </summary>
        public Tick ClientTick { get; set; }    // Synchronized

        public abstract void EncodeData(BitBuffer buffer);
        public abstract void DecodeData(BitBuffer buffer);

        public bool IsNewCommand { get; set; }
    }
}
