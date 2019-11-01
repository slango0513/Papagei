using MessagePack;

namespace Papagei
{
    /// <summary>
    /// States are attached to entities and contain user-defined data. They are
    /// responsible for encoding and decoding that data, and delta-compression.
    /// </summary>
    [MessagePackObject]
    public abstract class Event : IPoolable<Event>
    {
        public Event()
        {
        }

        public const int SEND_RELIABLE = -1;

        // #region Pooling
        [IgnoreMember]
        public IPool<Event> Pool { get; set; }

        public virtual void Reset()
        {
            EventId = SequenceId.INVALID;
            EntityId = EntityId.INVALID;
            Attempts = 0;
        }
        // #endregion

        // Settings
        protected virtual bool CanSendToFrozen => false;

        // Bindings
        [IgnoreMember]
        public Controller Sender { get; internal set; }

        // Synchronized
        [Key(1)]
        internal SequenceId EventId { get; set; }
        [Key(2)]
        public EntityId EntityId { get; set; }

        // Local only
        internal int Attempts { get; set; }

        internal bool IsReliable => Attempts == SEND_RELIABLE;
        internal bool CanSend => (Attempts > 0) || IsReliable;

        public virtual void SetDataFrom(Event other)
        {
        }

        public abstract void EncodeData(BitBuffer buffer, Tick packetTick);
        public abstract void DecodeData(BitBuffer buffer, Tick packetTick);

        [Key(0)]
        public int TypeCode { get; set; }
    }
}
