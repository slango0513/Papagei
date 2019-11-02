using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    /// <summary>
    /// Use this to control which entities update relative to one another.
    /// </summary>
    public enum UpdateOrder
    {
        Early,
        Normal,
        Late,
    }

    /// <summary>
    /// Server is the core executing class for communication. It is responsible
    /// for managing connection contexts and payload I/O.
    /// </summary>
    public abstract class World<T> where T : Entity
    {
        /// <summary>
        /// Fired before all entities have updated, for updating global logic.
        /// </summary>
        public event Action<Tick> World_PreUpdate = _ => { };

        /// <summary>
        /// Fired after all entities have updated, for updating global logic.
        /// </summary>
        public event Action<Tick> World_PostUpdate = _ => { };

        /// <summary>
        /// The current synchronized tick. On clients this will be the predicted
        /// server tick. On the server this will be the authoritative tick.
        /// </summary>
        public Tick World_Tick { get; set; } = Tick.INVALID;
        public Dictionary<EntityId, T> World_Entities { get; } = new Dictionary<EntityId, T>(EntityId.Comparer);

        // Pre-allocated removal list
        private readonly List<EntityId> _idsToRemove = new List<EntityId>();
        // Pre-cache the array for iterating over.
        private readonly UpdateOrder[] _orders = new[] { UpdateOrder.Early, UpdateOrder.Normal, UpdateOrder.Late };

        public void World_Update(Tick tick, Action<T> onUpdate, Action<T> onShutdown)
        {
            World_Tick = tick;

            World_PreUpdate.Invoke(World_Tick);

            foreach (var entity in GetAllEntities())
            {
                var removedTick = entity.RemovedTick;
                if (removedTick.IsValid && removedTick <= World_Tick)
                {
                    _idsToRemove.Add(entity.Id);
                }
                else
                {
                    onUpdate?.Invoke(entity);
                }
            }

            // Cleanup all entities marked for removal
            foreach (var id in _idsToRemove)
            {
                //RemoveEntity(id);
                if (World_Entities.TryGetValue(id, out var entity))
                {
                    World_Entities.Remove(id);
                    onShutdown?.Invoke(entity);
                }
            }

            _idsToRemove.Clear();

            World_PostUpdate.Invoke(World_Tick);

            IEnumerable<T> GetAllEntities()
            {
                // TODO: This makes multiple full passes, could probably optimize
                foreach (var order in _orders)
                {
                    foreach (var entity in World_Entities.Values)
                    {
                        if (entity.UpdateOrder == order)
                        {
                            yield return entity;
                        }
                    }
                }
            }
        }

        //protected readonly World<T> world = new World<T>();





        public Action<T> Entity_OnStart = _ => { };
        public Action<T> Entity_OnControllerChanged = _ => { };
        // Called on controller and server
        public Action<T, Command> Entity_ApplyControlGeneric = (_, __) => { };
        public Action<T> Entity_PostUpdate = _ => { };
        public Action<T> Entity_OnShutdown = _ => { };

        public Action<Event, Controller> Event_Invoke = (_, __) => { };
        public Action<Event, Controller, T> Event_InvokeEntity = (_, __, ___) => { };


        public event Action Started = () => { };

        private bool hasStarted = false;

        protected void DoStart()
        {
            if (!hasStarted)
            {
                Started.Invoke();
            }
            hasStarted = true;
        }

        /// <summary>
        /// Responsible for encoding and decoding packet information.
        /// </summary>
        //public class Interpreter

        /// <summary>
        /// Interpreter for converting byte input to a BitBuffer.
        /// </summary>
        //protected Interpreter Interpreter { get; } = new Interpreter();

        protected void ProcessPayload(Controller connection, Packet reusableIncoming, Func<T, bool> getSafeToExecute)
        {
            /// <summary>
            /// Records acknowledging information for the packet.
            /// </summary>
            // Connection.ProcessPacket
            connection.RemoteClock.UpdateLatest(reusableIncoming.SenderTick);
            foreach (var evnt in FilterIncomingEvents(reusableIncoming.Events))
            {
                /// <summary>
                /// Handles the execution of an incoming event.
                /// </summary>
                // private void ProcessEvent(Event evnt)
                {
                    if (evnt.EntityId.IsValid)
                    {
                        World_Entities.TryGetValue(evnt.EntityId, out var entity);

                        if (getSafeToExecute(entity))
                        {
                            Event_InvokeEntity(evnt, connection, entity);
                        }
                    }
                    else
                    {
                        Event_Invoke(evnt, connection);
                    }
                    connection.ProcessedEventHistory = connection.ProcessedEventHistory.Store(evnt.EventId);
                }
            }

            var ackedEventId = reusableIncoming.AckEventId;
            {
                /// <summary>
                /// Removes any acked or expired outgoing events.
                /// </summary>
                //void CleanOutgoingEvents(SequenceId ackedEventId)
                if (ackedEventId.IsValid != false)
                {
                    while (connection.OutgoingEvents.Count > 0)
                    {
                        var top = connection.OutgoingEvents.Peek();

                        // Stop if we hit an un-acked reliable event
                        if (top.IsReliable)
                        {
                            if (top.EventId > ackedEventId)
                            {
                                break;
                            }
                        }
                        // Stop if we hit an unreliable event with remaining attempts
                        else
                        {
                            if (top.Attempts > 0)
                            {
                                break;
                            }
                        }

                        var val = connection.OutgoingEvents.Dequeue();
                        val.Pool.Deallocate(val);
                    }
                }
            }

            /// <summary>
            /// Gets all events that we haven't processed yet, in order with no gaps.
            /// </summary>
            IEnumerable<Event> FilterIncomingEvents(IEnumerable<Event> events)
            {
                foreach (var evnt in events)
                {
                    if (connection.ProcessedEventHistory.IsNewId(evnt.EventId))
                    {
                        yield return evnt;
                    }
                }
            }
        }

        private Packet PrepareSend_(Controller connection, Packet reusableOutgoing, Tick localTick)
        {
            reusableOutgoing.Reset();
            reusableOutgoing.Initialize(localTick, connection.RemoteClock.LatestRemote, connection.ProcessedEventHistory.Latest, FilterOutgoingEvents(connection));
            return reusableOutgoing;
        }

        /// <summary>
        /// Selects outgoing events to send.
        /// </summary>
        protected IEnumerable<Event> FilterOutgoingEvents(Controller connection)
        {
            // The receiving client can only store SequenceWindow.HISTORY_LENGTH
            // events in its received buffer, and will skip any events older than
            // its latest received minus that history length, including reliable
            // events. In order to make sure we don't force the client to skip a
            // reliable event, we will throttle the outgoing events if we've been
            // sending them too fast. For example, if we have a reliable event
            // with ID 3 pending, the highest ID we can send would be ID 67. If we
            // send an event with ID 68, then the client may ignore ID 3 when it
            // comes in for being too old, even though it's reliable. 
            //
            // In practice this shouldn't be a problem unless we're sending way 
            // more events than is reasonable(/possible) in a single packet, or 
            // something is wrong with reliable event acking.

            var firstReliable = SequenceId.INVALID;
            foreach (var evnt in connection.OutgoingEvents)
            {
                if (evnt.IsReliable)
                {
                    if (!firstReliable.IsValid)
                    {
                        firstReliable = evnt.EventId;
                    }
                    Debug.Assert(firstReliable <= evnt.EventId);
                }

                if (firstReliable.IsValid)
                {
                    if (!SequenceWindow.AreInRange(firstReliable, evnt.EventId))
                    {
                        var current = "Throttling events due to unacked reliable\n";
                        foreach (var evnt2 in connection.OutgoingEvents)
                        {
                            current += evnt2.EventId + " ";
                        }
                        Console.WriteLine(current);
                        break;
                    }
                }

                if (evnt.CanSend)
                {
                    yield return evnt;
                }
            }
        }
    }
}
