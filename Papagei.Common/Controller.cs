using System;
using System.Collections.Generic;

namespace Papagei
{
    public abstract class Controller
    {
        public object UserData { get; set; }
        public Tick EstimatedRemoteTick => RemoteClock.EstimatedRemote;

        /// <summary>
        /// Queues an event to send directly to this peer.
        /// </summary>
        public void QueueEvent(Func<int, Event> eventFactory, Event evnt, int attempts)
        {
            // TODO: Event scoping

            // All global events are sent reliably
            //var clone = evnt.Clone();
            var clone = eventFactory(evnt.TypeCode);
            clone.EventId = evnt.EventId;
            clone.EntityId = evnt.EntityId;
            clone.Attempts = evnt.Attempts;
            clone.SetDataFrom(evnt);

            clone.EventId = lastQueuedEventId;
            clone.Attempts = attempts;

            OutgoingEvents.Enqueue(clone);
            lastQueuedEventId = lastQueuedEventId.Next;
        }

        /// <summary>
        /// An estimator for the remote peer's current tick.
        /// </summary>
        public Clock RemoteClock { get; } = new Clock();
        public IConnection Connection { get; }

        // #region Event-Related
        /// <summary>
        /// Used for uniquely identifying outgoing events.
        /// </summary>
        private SequenceId lastQueuedEventId = SequenceId.START.Next;

        public Queue<Event> OutgoingEvents { get; } = new Queue<Event>();

        public SequenceWindow ProcessedEventHistory { get; set; } = new SequenceWindow(SequenceId.START);
        // #endregion

        protected Controller(IConnection connection)
        {
            Connection = connection;
        }
    }
}
