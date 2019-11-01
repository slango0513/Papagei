using MessagePack;

namespace Papagei
{
    [MessagePackObject]
    public class StateDelta : IPoolable<StateDelta>, ITimedValue
    {
        // #region Pooling
        [IgnoreMember]
        public IPool<StateDelta> Pool { get; set; }

        public void Reset()
        {
            Tick = Tick.INVALID;
            EntityId = EntityId.INVALID;
            {
                // SafeReplace
                if (State != null)
                {
                    State.Pool.Deallocate(State);
                }
                State = null;
            }
            IsFrozen = false;
        }
        // #endregion

        // #region Interface
        [Key(0)]
        public Tick Tick { get; private set; } = Tick.INVALID;
        // #endregion

        [Key(1)]
        public EntityId EntityId { get; private set; } = EntityId.INVALID;
        [IgnoreMember]
        public State State { get; private set; } = default;
        [Key(2)]
        public bool IsFrozen { get; set; } = false;

        [IgnoreMember]
        public bool IsDestroyed => State.RemovedTick.IsValid;

        public void Initialize(Tick tick, EntityId entityId, State state, bool isFrozen)
        {
            Tick = tick;
            EntityId = entityId;
            State = state;
            IsFrozen = isFrozen;
        }
    }
}
