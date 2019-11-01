namespace Papagei
{
    /// <summary>
    /// Used to differentiate/typesafe state records. Not strictly necessary.
    /// </summary>
    public class StateRecord : IPoolable<StateRecord>, ITimedValue
    {
        // #region Pooling
        public IPool<StateRecord> Pool { get; set; }

        public void Reset()
        {
            Tick = Tick.INVALID;
            {
                // SafeReplace
                if (State != null)
                {
                    State.Pool.Deallocate(State);
                }
                State = null;
            }
        }
        // #endregion

        // #region Interface
        public Tick Tick { get; set; } = Tick.INVALID;
        // #endregion

        internal bool IsValid => Tick.IsValid;
        public State State { get; set; } = default;

        public void Invalidate()
        {
            Tick = Tick.INVALID;
        }
    }
}
