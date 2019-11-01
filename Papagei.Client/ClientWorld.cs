using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei.Client
{
    public class ClientWorld : World<ClientEntity>
    {
        // Client-only
        public Action<Entity> Entity_OnFrozen = _ => { };
        // Client-only
        public Action<Entity> Entity_OnUnfrozen = _ => { };
        // Called on non-controller client
        public Action<Entity> Entity_UpdateProxy = _ => { };
        // Called on controller
        public Action<Entity, Command> Entity_UpdateControlGeneric = (_, __) => { };
        public Action<Entity> Entity_Revert = _ => { };

        private readonly IClientPacketProtocol _protocol;
        public readonly ClientPools _pools;

        public ClientWorld(IClientPacketProtocol protocol, ClientPools pools)
        {
            _protocol = protocol;
            _pools = pools;
            World_Tick = Tick.INVALID;
        }

        private ClientController controller = default;

        /// <summary>
        /// Entities that are waiting to be added to the world.
        /// </summary>
        private readonly Dictionary<EntityId, ClientEntity> pendingEntities = new Dictionary<EntityId, ClientEntity>(EntityId.Comparer);

        /// <summary>
        /// All known entities, either in-world or pending.
        /// </summary>
        private readonly Dictionary<EntityId, ClientEntity> knownEntities = new Dictionary<EntityId, ClientEntity>(EntityId.Comparer);

        // The local simulation tick, used for commands
        private Tick localTick = Tick.START;

        // Pre-allocated removal list
        private readonly List<Entity> entitiesToRemove = new List<Entity>();

        private readonly ViewComparer _viewComparer = new ViewComparer();

        public void SetConnection(IConnection connection)
        {
            Debug.Assert(controller == null, "Overwriting peer");
            controller = new ClientController(connection);

            // ClientConnection.ctor
            controller.Connection.PayloadReceived += (data, length) =>
            {
                Console.WriteLine($"data {data.Length} length {length}"); // 1203 975
                var reusableIncoming = _protocol.Decode(data, length);
                if (reusableIncoming != default)
                {
                    ProcessPayload(controller, reusableIncoming, entity => { return entity != null; });
                    //onProcessPacket();
                    foreach (var delta in reusableIncoming.ReceivedDeltas)
                    {
                        controller.LocalView.RecordUpdate(delta.EntityId, new ViewEntry(reusableIncoming.SenderTick, delta.IsFrozen));
                    }
                    // serverPeer.PacketReceived.Invoke(serverPacket);
                    // serverPeer.PacketReceived += (serverPacket) =>
                    foreach (var delta in reusableIncoming.ReceivedDeltas)
                    {
                        // ClientManager.ProcessDelta
                        if (!knownEntities.TryGetValue(delta.EntityId, out var entity))
                        {
                            Debug.Assert(delta.IsFrozen == false, "Frozen unknown entity");
                            if (!delta.IsFrozen)
                            {
                                var typeCode = delta.State.TypeCode;
                                //entity = Entity.Create(factoryType);
                                entity = _pools.EntityFactories[typeCode].Allocate();
                                //entity.factoryType = factoryType;
                                entity.StateBase = _pools.CreateState(typeCode);

                                var authState = _pools.CreateState(entity.StateBase.TypeCode);
                                authState.OverwriteFrom(entity.StateBase);
                                entity.AuthStateBase = authState;

                                var nextState = _pools.CreateState(entity.StateBase.TypeCode);
                                nextState.OverwriteFrom(entity.StateBase);
                                entity.NextStateBase = nextState;


                                entity.Id = delta.EntityId;
                                pendingEntities.Add(entity.Id, entity);
                                knownEntities.Add(entity.Id, entity);
                            }
                        }
                        {
                            // Entity.ReceiveDelta
                            var stored = false;
                            if (delta.IsFrozen)
                            {
                                // Frozen deltas have no state data, so we need to treat them
                                // separately when doing checks based on state content
                                stored = entity.IncomingStates.Store(delta);
                            }
                            else
                            {
                                if (delta.IsDestroyed)
                                {
                                    entity.RemovedTick = delta.State.RemovedTick;
                                }
                                else
                                {
                                    stored = entity.IncomingStates.Store(delta);
                                }

                                if (delta.State.HasControllerData)
                                {
                                    var ackTick = delta.State.CommandAck;
                                    {
                                        // Entity.CleanCommands(delta.CommandAck);
                                        if (ackTick.IsValid != false)
                                        {
                                            while (entity.OutgoingCommands.Count > 0)
                                            {
                                                var command = entity.OutgoingCommands.Peek();
                                                if (command.ClientTick > ackTick)
                                                {
                                                    break;
                                                }

                                                var val = entity.OutgoingCommands.Dequeue();
                                                val.Pool.Deallocate(val);
                                            }
                                        }
                                    }
                                }
                            }

                            // We never stored it, so free the delta
                            if (!stored)
                            {
                                delta.Pool.Deallocate(delta);
                            }
                        }

                        {
                            // ClientManager.UpdateControlStatus
                            // Can't infer anything if the delta is an empty frozen update
                            if (!delta.IsFrozen)
                            {
                                Debug.Assert(delta.State != null);
                                if (delta.State.HasControllerData)
                                {
                                    if (entity.Controller == null)
                                    {
                                        controller.GrantControl(entity);
                                    }
                                }
                                else
                                {
                                    if (entity.Controller != null)
                                    {
                                        controller.RevokeControl(entity);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Bad packet read, discarding...");
                }
                reusableIncoming.Reset();
            };
        }

        /// <summary>
        /// Queues an event to broadcast to all clients.
        /// Use a Event.SEND_RELIABLE (-1) for the number of attempts
        /// to send the event reliable-ordered (infinite retries).
        /// </summary>
        public void QueueEvent(Event evnt, int attempts = 3)
        {
            controller.QueueEvent(typeCode => _pools.CreateEvent(typeCode), evnt, attempts);
        }

        public void Update()
        {
            if (controller == null)
            {
                return;
            }

            DoStart();
            controller.RemoteClock.Update();

            /// <summary>
            /// Updates the room a number of ticks. If we have entities waiting to be
            /// added, this function will check them and add them if applicable.
            /// </summary>
            // ClientManager.UpdateRoom
            var estimatedServerTick = controller.EstimatedRemoteTick;

            {
                /// <summary>
                /// Checks to see if any pending entities can be added to the world and
                /// adds them if applicable.
                /// </summary>
                // ClientManager.UpdatePendingEntities
                //var serverTick = estimatedServerTick;
                foreach (var entity in pendingEntities.Values)
                {
                    var hasReadyState = entity.IncomingStates.GetLatestAt(estimatedServerTick) != null;
                    if (hasReadyState)
                    {
                        //world.AddEntity(entity);
                        World_Entities.Add(entity.Id, entity);
                        entitiesToRemove.Add(entity);
                    }
                }

                foreach (var entity in entitiesToRemove)
                {
                    pendingEntities.Remove(entity.Id);
                }

                entitiesToRemove.Clear();
            }

            // ClientWorld.ClientUpdate
            // Perform regular update cadence and mark entities for removal
            World_Update(estimatedServerTick, entity =>
            {
                {
                    // Entity.UpdateAuthState
                    // Apply all un-applied deltas to the auth state
                    var toApply = entity.IncomingStates.GetRangeAndNext(entity.AuthTick, World_Tick, out var next);

                    foreach (var delta in toApply)
                    {
                        if (entity.AuthTick == Tick.START)
                        {
                            Debug.Assert(delta.State.HasImmutableData);
                        }

                        if (!delta.IsFrozen)
                        {
                            StateUtils.ApplyDelta(entity.AuthStateBase, delta);
                        }

                        entity.ShouldBeFrozen = delta.IsFrozen;
                        entity.AuthTick = delta.Tick;
                    }

                    // If there was a next state, update the next state
                    var canGetNext = !entity.ShouldBeFrozen;
                    if (canGetNext && next != null && !next.IsFrozen)
                    {
                        entity.NextStateBase.OverwriteFrom(entity.AuthStateBase);
                        StateUtils.ApplyDelta(entity.NextStateBase, next);
                        entity.NextTick = next.Tick;
                    }
                    else
                    {
                        entity.NextTick = Tick.INVALID;
                    }
                }

                entity.StateBase.OverwriteFrom(entity.AuthStateBase);
                //entity.Initialize();
                if (!entity.HasStarted)
                {
                    Entity_OnStart(entity);
                }
                entity.HasStarted = true;

                //entity.NotifyControllerChanged();
                if (entity.DeferNotifyControllerChanged)
                {
                    Entity_OnControllerChanged(entity);
                }
                entity.DeferNotifyControllerChanged = false;

                {
                    // Entity.SetFreeze
                    var shouldBeFrozen = entity.ShouldBeFrozen;
                    if (!entity.IsFrozen && shouldBeFrozen)
                    {
                        Entity_OnFrozen(entity);
                    }
                    else if (entity.IsFrozen && !shouldBeFrozen)
                    {
                        Entity_OnUnfrozen(entity);
                    }

                    entity.IsFrozen = shouldBeFrozen;
                }

                if (!entity.IsFrozen)
                {
                    if (entity.Controller == null)
                    {
                        Entity_UpdateProxy(entity);
                    }
                    else
                    {
                        entity.NextTick = Tick.INVALID;

                        // Entity.UpdateControlled
                        Debug.Assert(entity.Controller != null);
                        if (entity.OutgoingCommands.Count < Config.COMMAND_BUFFER_COUNT)
                        {
                            var command = _pools.CommandPool.Allocate();

                            command.ClientTick = localTick;
                            command.IsNewCommand = true;

                            Entity_UpdateControlGeneric(entity, command);
                            entity.OutgoingCommands.Enqueue(command);
                        }

                        // Entity.UpdatePredicted
                        // Bring the main state up to the latest (apply all deltas)
                        var deltas = entity.IncomingStates.GetRange(entity.AuthTick);
                        foreach (var delta in deltas)
                        {
                            StateUtils.ApplyDelta(entity.StateBase, delta);
                        }

                        Entity_Revert(entity);

                        // Forward-simulate
                        foreach (var command in entity.OutgoingCommands)
                        {
                            Entity_ApplyControlGeneric(entity, command);
                            command.IsNewCommand = false;
                        }
                    }

                    Entity_PostUpdate(entity);
                }
            },
            entity => Entity_OnShutdown(entity));

            if (localTick.IsSendTick())
            {
                // TODO: Sort me by most recently sent
                var controlledEntities = controller.ControlledEntities;
                {
                    // internal void SendPacket(Tick localTick, IEnumerable<Entity> controlledEntities)
                    // Packet.PrepareSend
                    //var packet_ = controller.ReusableOutgoing;
                    //packet_.Reset();
                    var packet_ = new ClientOutgoingPacket();
                    packet_.Initialize(localTick, controller.RemoteClock.LatestRemote, controller.ProcessedEventHistory.Latest, FilterOutgoingEvents(controller));

                    var commandUpdates = ProduceCommandUpdates(controlledEntities);
                    {
                        // public void Populate(IEnumerable<CommandUpdate> commandUpdates, View view)
                        packet_.PendingCommandUpdates.AddRange(commandUpdates);
                        // Integrate
                        foreach (var pair in controller.LocalView.LatestUpdates)
                        {
                            packet_.View.RecordUpdate(pair.Key, pair.Value);
                        }
                    }

                    Order(packet_.View);

                    /// <summary>
                    /// Views sort in descending tick order. When sending a view to the server
                    /// we send the most recent updated entities since they're the most likely
                    /// to actually matter to the server/client scope.
                    /// </summary>
                    void Order(View view)
                    {
                        // TODO: If we have an entity frozen, we probably shouldn't constantly
                        //       send view acks for it unless we're getting requests to freeze.
                        view.SortList.Clear();
                        view.SortList.AddRange(view.LatestUpdates);
                        view.SortList.Sort(_viewComparer);
                        view.SortList.Reverse();
                    }


                    // Send the packet
                    var t = _protocol.Encode(packet_);
                    controller.Connection.SendPayload(t.Item1, t.Item2);


                    foreach (var commandUpdate in packet_.SentCommandUpdates)
                    {
                        commandUpdate.Entity.LastSentCommandTick = localTick;
                    }
                }
            }

            localTick++;
        }

        private readonly TickComparer _comparer = new TickComparer();
        private IEnumerable<ClientCommandUpdate> ProduceCommandUpdates(IEnumerable<ClientEntity> entities)
        {
            // If we have too many entities to fit commands for in a packet,
            // we want to round-robin sort them to avoid starvation
            controller.SortingList.Clear();
            controller.SortingList.AddRange(entities);
            controller.SortingList.Sort((x, y) => _comparer.Compare(x.LastSentCommandTick, y.LastSentCommandTick));

            foreach (var entity in controller.SortingList)
            {
                // CommandUpdate.Create
                var update = _pools.CommandUpdatePool.Allocate();
                {
                    // CommandUpdate.Initialize(entity.Id, entity.OutgoingCommands);
                    update.EntityId = entity.Id;
                    foreach (var command in entity.OutgoingCommands)
                    {
                        update.Commands.Store(command);
                    }
                }

                update.Entity = entity;
                yield return update;
            }
        }
    }

    public static class StateUtils
    {
        public static void ApplyDelta(State state, StateDelta delta)
        {
            var deltaState = delta.State;
            state.ApplyMutableFrom(deltaState, deltaState.Flags);

            state.ResetControllerData();
            if (deltaState.HasControllerData)
            {
                state.ApplyControllerFrom(deltaState);
            }

            state.HasControllerData = deltaState.HasControllerData;

            state.HasImmutableData = deltaState.HasImmutableData || state.HasImmutableData;
            if (deltaState.HasImmutableData)
            {
                state.ApplyImmutableFrom(deltaState);
            }
        }
    }
}
