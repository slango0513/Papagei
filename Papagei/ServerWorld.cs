using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    /// <summary>
    /// Server is the core executing class on the server. It is responsible for
    /// managing connection contexts and payload I/O.
    /// </summary>
    public class ServerWorld : World<ServerEntity>
    {
        // Called on server
        public Action<Entity> Entity_UpdateAuth = _ => { };

        private readonly IServerPacketProtocol _protocol;
        public readonly ServerPools _pools;

        public ServerWorld(IServerPacketProtocol protocol, ServerPools pools)
        {
            _protocol = protocol;
            _pools = pools;
            World_Tick = Tick.START;
        }

        /// <summary>
        /// Fired when a controller has been added (i.e. player join).
        /// The controller has control of no entities at this point.
        /// </summary>
        public event Action<ServerController> ControllerJoined = _ => { };

        /// <summary>
        /// Fired when a controller has been removed (i.e. player leave).
        /// This event fires before the controller has control of its entities
        /// revoked (this is done immediately afterwards).
        /// </summary>
        public event Action<ServerController> ControllerLeft = _ => { };

        /// <summary>
        /// Collection of all participating clients.
        /// </summary>
        private readonly Dictionary<IConnection, ServerController> clients = new Dictionary<IConnection, ServerController>();

        /// <summary>
        /// Entities that have been destroyed.
        /// </summary>
        private readonly Dictionary<EntityId, ServerEntity> destroyedEntities = new Dictionary<EntityId, ServerEntity>();

        /// <summary>
        /// Used for creating new entities and assigning them unique ids.
        /// </summary>
        private EntityId nextEntityId = EntityId.START;

        /// <summary>
        /// Wraps an incoming connection in a peer and stores it.
        /// </summary>
        public void AddConnection(IConnection connection)
        {
            if (!clients.ContainsKey(connection))
            {
                var controller = new ServerController(connection);

                // ServerConnection.ctor
                controller.Connection.PayloadReceived += (data, length) =>
                {
                    Console.WriteLine($"data {data.Length} length {length}"); // 253 245
                    var reusableIncoming = _protocol.Decode(data, length);
                    if (reusableIncoming != default)
                    {
                        ProcessPayload(controller, reusableIncoming, entity =>
                        {
                            // Entity events can only be executed on controlled entities
                            return entity != null && entity.Controller == controller;
                        });
                        //onProcessPacket();
                        // Integrate
                        foreach (var pair in reusableIncoming.View.LatestUpdates)
                        {
                            controller.Scope.AckedByClient.RecordUpdate(pair.Key, pair.Value);
                        }

                        // client.PacketReceived.Invoke(client, clientPacket);
                        {
                            // client.PacketReceived += (_peer, clientPacket) =>
                            foreach (var update in reusableIncoming.ReceivedCommandUpdates)
                            {
                                {
                                    // ServerManager.ProcessCommandUpdate
                                    if (World_Entities.TryGetValue(update.EntityId, out var entity))
                                    {
                                        foreach (var command in update.Commands.GetValues())
                                        {
                                            if (entity.Controller == controller && entity.IncomingCommands.Store(command))
                                            {
                                                command.IsNewCommand = true;
                                            }
                                            else
                                            {
                                                command.Pool.Deallocate(command);
                                            }
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

                clients.Add(connection, controller);

                ControllerJoined.Invoke(controller);
            }
        }

        /// <summary>
        /// Wraps an incoming connection in a peer and stores it.
        /// </summary>
        public void RemovePeer(IConnection peer)
        {
            if (clients.ContainsKey(peer))
            {
                var client = clients[peer];
                clients.Remove(peer);

                ControllerLeft.Invoke(client);

                // Revoke control of all the entities controlled by that controller
                {
                    // ServerConnection.Shutdown
                    foreach (var entity in client.ControlledEntities)
                    {
                        entity.Controller = null;
                        entity.IncomingCommands.Clear();
                        entity.DeferNotifyControllerChanged = true;
                    }
                    client.ControlledEntities.Clear();
                }
            }
        }

        /// <summary>
        /// Creates an entity of a given type and adds it to the world.
        /// </summary>
        public Entity AddNewEntity(Type type)
        {
            int typeCode = _pools.EntityTypeToKey[type];
            //var entity = Entity.Create(factoryType);
            var entity = _pools.EntityFactories[typeCode].Allocate();
            //entity.factoryType = factoryType;
            entity.StateBase = _pools.CreateState(typeCode);

            entity.Id = nextEntityId;
            nextEntityId = nextEntityId.GetNext();
            //world.AddEntity(entity);
            World_Entities.Add(entity.Id, entity);
            return entity;
        }

        /// <summary>
        /// Removes an entity from the world and destroys it.
        /// </summary>
        public void DestroyEntity(ServerEntity entity)
        {
            if (entity.Controller != null)
            {
                var serverController = entity.Controller;
                serverController.RevokeControl(entity);
            }

            // IsRemoving
            if (!entity.RemovedTick.IsValid)
            {
                {
                    // Entity.MarkForRemove
                    // We'll remove on the next tick since we're probably 
                    // already mid-way through evaluating this tick
                    entity.RemovedTick = World_Tick + 1;
                }
                destroyedEntities.Add(entity.Id, entity);
            }
        }

        /// <summary>
        /// Queues an event to broadcast to all clients.
        /// Use a Event.SEND_RELIABLE (-1) for the number of attempts
        /// to send the event reliable-ordered (infinite retries).
        /// </summary>
        public void QueueEventBroadcast(Event evnt, int attempts = 3)
        {
            foreach (var clientPeer in clients.Values)
            {
                clientPeer.QueueEvent(typeCode => _pools.CreateEvent(typeCode), evnt, attempts);
            }
        }

        /// <summary>
        /// Updates all entities and dispatches a snapshot if applicable. Should
        /// be called once per game simulation tick (e.g. during Unity's 
        /// FixedUpdate pass).
        /// </summary>
        public void Update()
        {
            DoStart();

            foreach (var client in clients.Values)
            {
                client.RemoteClock.Update();
            }

            {
                // ServerWorld.ServerUpdate
                var tick = World_Tick.GetNext();

                World_Update(tick, entity =>
                {
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

                    Entity_UpdateAuth(entity);

                    Command latest = default;
                    if (entity.Controller != null)
                    {
                        latest = entity.IncomingCommands.GetLatestAt(entity.Controller.EstimatedRemoteTick);
                    }

                    if (latest != null)
                    {
                        Entity_ApplyControlGeneric(entity, latest);
                        latest.IsNewCommand = false;

                        var latestCommandTick = entity.Controller.EstimatedRemoteTick;
                        // Use the remote tick rather than the last applied tick
                        // because we might be skipping some commands to keep up
                        var shouldAck = (entity.CommandAck.IsValid == false) || (latestCommandTick > entity.CommandAck);
                        if (shouldAck)
                        {
                            entity.CommandAck = latestCommandTick;
                        }
                    }

                    Entity_PostUpdate(entity);
                },
                entity => Entity_OnShutdown(entity));
            }

            if (World_Tick.IsSendTick())
            {
                {
                    // ServerWorld.StoreStates
                    foreach (var entity in World_Entities.Values)
                    {
                        {
                            // Entity.StoreRecord();
                            var record = CreateRecord(World_Tick, entity.StateBase, entity.OutgoingStates.Latest);

                            /// <summary>
                            /// Creates a record of the current state, taking the latest record (if
                            /// any) into account. If a latest state is given, this function will
                            /// return null if there is no change between the current and latest.
                            /// </summary>
                            StateRecord CreateRecord(Tick tick, State current, StateRecord latestRecord = null)
                            {
                                if (latestRecord != null)
                                {
                                    var latest = latestRecord.State;
                                    var shouldReturn = current.CompareMutableData(latest) > 0 || !current.IsControllerDataEqual(latest);
                                    if (!shouldReturn)
                                    {
                                        return null;
                                    }
                                }

                                var _record = _pools.RecordPool.Allocate();
                                {
                                    //record.Overwrite(tick, current);
                                    Debug.Assert(tick.IsValid);

                                    _record.Tick = tick;
                                    if (_record.State == null)
                                    {
                                        //this.state = state.Clone();
                                        var clone = _pools.CreateState(current.TypeCode);
                                        clone.OverwriteFrom(current);
                                        _record.State = clone;
                                    }
                                    else
                                    {
                                        _record.State.OverwriteFrom(current);
                                    }
                                }
                                return _record;
                            }

                            if (record != null)
                            {
                                entity.OutgoingStates.Store(record);
                            }
                        }
                    }
                }
                {
                    /// <summary>
                    /// Packs and sends a server-to-client packet to each peer.
                    /// </summary>
                    // ServerManager.BroadcastPackets
                    foreach (var controller in clients.Values)
                    {
                        var localTick = World_Tick;
                        var active = World_Entities.Values;
                        var destroyed = destroyedEntities.Values;
                        {
                            //ServerConnection.SendPacket
                            /// <summary>
                            /// Allocates a packet and writes common boilerplate information to it.
                            /// Make sure to call OnSent() afterwards.
                            /// </summary>
                            // Packet.PrepareSend
                            //var packet_ = controller.ReusableOutgoing;
                            //packet_.Reset();
                            var packet_ = new ServerOutgoingPacket();
                            packet_.Initialize(localTick, controller.RemoteClock.LatestRemote, controller.ProcessedEventHistory.Latest, FilterOutgoingEvents(controller));

                            controller.Scope.PopulateDeltas(controller, localTick, packet_, active, destroyed, () => _pools.DeltaPool.Allocate(), typeCode => _pools.CreateState(typeCode));

                            var t = _protocol.Encode(packet_);
                            controller.Connection.SendPayload(t.Item1, t.Item2);


                            foreach (var delta in packet_.SentDeltas)
                            {
                                controller.Scope.LastSent.RecordUpdate(delta.EntityId, new ViewEntry(localTick, delta.IsFrozen));
                            }
                        }
                    }
                }
            }
        }
    }
}
