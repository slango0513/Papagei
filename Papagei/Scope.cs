using System;
using System.Collections.Generic;

namespace Papagei
{
    public class EntityPriorityComparer : Comparer<KeyValuePair<float, ServerEntity>>
    {
        private readonly Comparer<float> _comparer = Comparer<float>.Default;

        public override int Compare(KeyValuePair<float, ServerEntity> x, KeyValuePair<float, ServerEntity> y)
        {
            return _comparer.Compare(x.Key, y.Key);
        }
    }

    public class Scope
    {
        private readonly EntityPriorityComparer _comparer = new EntityPriorityComparer();

        public IScopeEvaluator Evaluator { get; set; } = new DefaultScopeEvaluator();

        public View LastSent { get; } = new View();

        public View AckedByClient { get; } = new View();

        // Pre-allocated reusable fill lists
        private readonly List<KeyValuePair<float, ServerEntity>> entryList = new List<KeyValuePair<float, ServerEntity>>();
        private readonly List<StateDelta> activeList = new List<StateDelta>();
        private readonly List<StateDelta> frozenList = new List<StateDelta>();
        private readonly List<StateDelta> destroyedList = new List<StateDelta>();

        public void PopulateDeltas(Controller target, Tick serverTick, ServerOutgoingPacket packet, IEnumerable<ServerEntity> activeEntities, IEnumerable<ServerEntity> destroyedEntities,
            Func<StateDelta> deltaFactory, Func<int, State> stateFactory)
        {
            {
                /// <summary>
                /// Divides the active entities into those that are in scope and those
                /// out of scope. If an entity is out of scope and hasn't been acked as
                /// such by the client, we will add it to the outgoing frozen delta list.
                /// Otherwise, if an entity is in scope we will add it to the sorted
                /// active delta list.
                /// </summary>
                // void ProduceScoped(IController target, Tick serverTick, IEnumerable<Entity> activeEntities)
                entryList.Clear();

                foreach (var entity in activeEntities)
                {
                    // Controlled entities are always in scope with highest priority
                    if (entity.Controller == target)
                    {
                        entryList.Add(new KeyValuePair<float, ServerEntity>(float.MinValue, entity));
                    }
                    else
                    {
                        var b = GetPriority(entity, serverTick, out var priority);
                        if (b)
                        {
                            entryList.Add(new KeyValuePair<float, ServerEntity>(priority, entity));
                        }
                        else
                        {
                            // We only want to send a freeze state if we aren't already frozen
                            var latest = AckedByClient.GetLatest(entity.Id);
                            if (!latest.IsFrozen)
                            {
                                // StateDelta.CreateFrozen
                                var delta = deltaFactory();
                                delta.Initialize(serverTick, entity.Id, null, true);

                                frozenList.Add(delta);
                            }
                        }
                    }
                }

                entryList.Sort(_comparer);
                foreach (var entry in entryList)
                {
                    var latest = AckedByClient.GetLatest(entry.Value.Id);
                    var delta = ProduceDelta(deltaFactory, stateFactory, entry.Value, latest.Tick, target);
                    if (delta != null)
                    {
                        activeList.Add(delta);
                    }
                }
            }
            {
                /// <summary>
                /// Produces deltas for all non-acked destroyed entities.
                /// </summary>
                // private void ProduceDestroyed(IController target, IEnumerable<Entity> destroyedEntities)
                foreach (var entity in destroyedEntities)
                {
                    var latest = AckedByClient.GetLatest(entity.Id);
                    if (latest.Tick.IsValid && (latest.Tick < entity.RemovedTick))
                    {
                        // Note: Because the removed tick is valid, this should force-create
                        var delta = ProduceDelta(deltaFactory, stateFactory, entity, latest.Tick, target);
                        destroyedList.Add(delta);
                    }
                }
            }

            {
                // packet.Populate(activeList, frozenList, destroyedList);
                packet.PendingDeltas.AddRange(destroyedList);
                packet.PendingDeltas.AddRange(frozenList);
                packet.PendingDeltas.AddRange(activeList);
            }

            destroyedList.Clear();
            frozenList.Clear();
            activeList.Clear();
        }

        private bool GetPriority(Entity entity, Tick current, out float priority)
        {
            var lastSent = LastSent.GetLatest(entity.Id);
            return lastSent.Tick.IsValid
                ? Evaluator.Evaluate(entity, current - lastSent.Tick, out priority)
                : Evaluator.Evaluate(entity, int.MaxValue, out priority);
        }

        public static StateDelta ProduceDelta(Func<StateDelta> deltaFactory, Func<int, State> stateFactory,
            ServerEntity entity, Tick basisTick, Controller destination)
        {
            StateRecord basis = null;
            if (basisTick.IsValid)
            {
                basis = entity.OutgoingStates.LatestAt(basisTick);
            }

            // Flags for special data modes
            var includeControllerData = destination == entity.Controller;
            var includeImmutableData = !basisTick.IsValid;

            return CreateDelta(deltaFactory, stateFactory, entity.Id, entity.StateBase, basis, includeControllerData, includeImmutableData, entity.CommandAck, entity.RemovedTick);
        }

        /// <summary>
        /// Creates a delta between a state and a record. If forceUpdate is set 
        /// to false, this function will return null if there is no change between
        /// the current and basis.
        /// </summary>
        private static StateDelta CreateDelta(Func<StateDelta> deltaFactory, Func<int, State> stateFactory,
            EntityId entityId, State current, StateRecord basisRecord, bool includeControllerData, bool includeImmutableData, Tick commandAck, Tick removedTick)
        {
            var shouldReturn = includeControllerData || includeImmutableData || removedTick.IsValid;

            var flags = State.FLAGS_ALL;
            if (basisRecord != null && basisRecord.State != null)
            {
                flags = current.CompareMutableData(basisRecord.State);
            }

            if (flags == 0 && !shouldReturn)
            {
                return null;
            }

            var deltaState = stateFactory(current.TypeCode);
            deltaState.Flags = flags;
            deltaState.ApplyMutableFrom(current, deltaState.Flags);

            deltaState.HasControllerData = includeControllerData;
            if (includeControllerData)
            {
                deltaState.ApplyControllerFrom(current);
            }

            deltaState.HasImmutableData = includeImmutableData;
            if (includeImmutableData)
            {
                deltaState.ApplyImmutableFrom(current);
            }

            deltaState.RemovedTick = removedTick;
            deltaState.CommandAck = commandAck;

            // We don't need to include a tick when sending -- it's in the packet
            var delta = deltaFactory();
            delta.Initialize(Tick.INVALID, entityId, deltaState, false);
            return delta;
        }
    }
}
