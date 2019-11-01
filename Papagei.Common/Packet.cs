using MessagePack;
using System.Collections.Generic;

namespace Papagei
{
    public abstract class Packet
    {
        public virtual void Reset()
        {
            SenderTick = Tick.INVALID;
            AckTick = Tick.INVALID;
            AckEventId = SequenceId.INVALID;

            PendingEvents.Clear();
            Events.Clear();
            EventsWritten = 0;
        }

        /// <summary>
        /// The latest tick from the sender.
        /// </summary>
        [Key(0)]
        public Tick SenderTick { get; set; } = Tick.INVALID;

        /// <summary>
        /// The last tick the sender received.
        /// </summary>
        [Key(1)]
        public Tick AckTick { get; set; } = Tick.INVALID;

        /// <summary>
        /// The last global reliable event id the sender received.
        /// </summary>
        [Key(2)]
        public SequenceId AckEventId { get; set; } = SequenceId.INVALID;

        /// <summary>
        /// Global reliable events from the sender, in order.
        /// </summary>
        [Key(3)]
        public List<Event> Events { get; } = new List<Event>();

        [Key(4)]
        public List<Event> PendingEvents { get; } = new List<Event>();

        [Key(5)]
        public int EventsWritten { get; set; } = 0;

        public void Initialize(Tick senderTick, Tick ackTick, SequenceId ackEventId, IEnumerable<Event> events)
        {
            SenderTick = senderTick;
            AckTick = ackTick;
            AckEventId = ackEventId;
            PendingEvents.AddRange(events);
            EventsWritten = 0;
        }
    }
}
