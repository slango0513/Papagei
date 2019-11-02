using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Papagei;
using Papagei.Client;
using System;
using System.Collections.Generic;

namespace Playground.Client
{
    public class ClientProtocol : BitBufferClientPacketProtocol
    {
        public ClientProtocol(ClientPools pools) : base(pools)
        {
        }

        public override void DecodeControllerData(State state)
        {
            switch (state)
            {
                case MyState state_:
                    {
                        break;
                    }
                case DummyEntityState state_:
                    {
                        break;
                    }
                default:
                    break;
            }
        }

        public override void DecodeImmutableData(State state)
        {
            switch (state)
            {
                case MyState state_:
                    {
                        state_.ArchetypeId = _buffer.ReadInt();
                        state_.UserId = _buffer.ReadInt();
                        break;
                    }
                case DummyEntityState state_:
                    {
                        state_.ArchetypeId = _buffer.ReadInt();
                        state_.UserId = _buffer.ReadInt();
                        break;
                    }
                default:
                    break;
            }
        }

        public override void DecodeMutableData(uint flags, State state)
        {
            switch (state)
            {
                case MyState state_:
                    {
                        var _flags = (MyState.Props)flags;
                        if (_flags.HasFlag(MyState.Props.X))
                        {
                            state_.X = _buffer.ReadFloat(GameCompressors.Coordinate);
                        }
                        if (_flags.HasFlag(MyState.Props.Y))
                        {
                            state_.Y = _buffer.ReadFloat(GameCompressors.Coordinate);
                        }
                        if (_flags.HasFlag(MyState.Props.Angle))
                        {
                            state_.Angle = _buffer.ReadFloat(GameCompressors.Angle);
                        }
                        if (_flags.HasFlag(MyState.Props.Status))
                        {
                            state_.Status = _buffer.ReadByte();
                        }
                        break;
                    }
                case DummyEntityState state_:
                    {
                        var _flags = (DummyEntityState.Props)flags;
                        if (_flags.HasFlag(DummyEntityState.Props.X))
                        {
                            state_.X = _buffer.ReadFloat(GameCompressors.Coordinate);
                        }
                        if (_flags.HasFlag(DummyEntityState.Props.Y))
                        {
                            state_.Y = _buffer.ReadFloat(GameCompressors.Coordinate);
                        }
                        if (_flags.HasFlag(DummyEntityState.Props.Z))
                        {
                            state_.Z = _buffer.ReadFloat(GameCompressors.Coordinate);
                        }
                        if (_flags.HasFlag(DummyEntityState.Props.Angle))
                        {
                            state_.Angle = _buffer.ReadFloat(GameCompressors.Angle);
                        }
                        if (_flags.HasFlag(DummyEntityState.Props.Status))
                        {
                            state_.Status = _buffer.ReadByte();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public override void EncodeData(Command command)
        {
            switch (command)
            {
                case GameCommand command_:
                    {
                        _buffer.WriteBool(command_.Up);
                        _buffer.WriteBool(command_.Down);
                        _buffer.WriteBool(command_.Left);
                        _buffer.WriteBool(command_.Right);
                        _buffer.WriteBool(command_.Action);
                        break;
                    }
                default:
                    break;
            }
        }
    }

    public class ClientWorldHost
    {
        public IServiceProvider ServiceProvider { get; }

        public ClientWorldHost()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var commandType = typeof(GameCommand);
            IEnumerable<Type> eventTypes = new List<Type>
            {
                typeof(GameActionEvent),
            };
            IEnumerable<KeyValuePair<Type, Type>> entityTypes = new Dictionary<Type, Type>
            {
                [typeof(ClientControlledEntity)] = typeof(MyState),
                [typeof(ClientDummyEntity)] = typeof(DummyEntityState),
                [typeof(ClientMimicEntity)] = typeof(MyState),
            };
            var pools = new ClientPools(commandType, eventTypes, entityTypes);
            var manager = new ClientWorld(new ClientProtocol(pools), pools);
            //var manager = new ClientWorld(new MessagePackClientPacketProtocol(), pools);
            services.AddSingleton(manager);

            ServiceProvider = services.BuildServiceProvider();

            InitializeCommon();
            Initialize();
        }

        public Action<ClientControlledEntity> Client_OnControlledCreated = _ => { };
        public Action<ClientDummyEntity> Client_OnDummyCreated = _ => { };
        public Action<ClientMimicEntity> Client_OnMimicCreated = _ => { };

        public Action<GameActionEvent> Client_OnGameActionEvent = _ => { };
        public Action<GameActionEvent, Entity> Client_OnGameActionEventEntity = (_, __) => { };

        private void InitializeCommon()
        {
            var manager = ServiceProvider.GetRequiredService<ClientWorld>();
            manager.Entity_OnStart += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            Client_OnControlledCreated(entity_);
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            Client_OnDummyCreated(entity_);

                            //entity_.startX = entity_.State.X;
                            //entity_.startY = entity_.State.Y;
                            //entity_.startZ = entity_.State.Z;
                            //entity_.angle = 0.0f;

                            //entity_.distance = 1.0f + ((float)entity_.random.NextDouble() * 2.0f);
                            //entity_.speed = 1.0f + ((float)entity_.random.NextDouble() * 2.0f);

                            //if (entity_.random.NextDouble() > 0.5f)
                            //{
                            //    entity_.speed *= -1.0f;
                            //}
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            Client_OnMimicCreated(entity_);
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_OnControllerChanged += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_ApplyControlGeneric += (_entity, command) =>
            {
                switch (_entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            // 预处理
                            var _toApply = (GameCommand)command;
                            if (_toApply.Up)
                            {
                                entity_.State.Y += 5.0f * 0.02f /*Time.fixedDeltaTime*/;
                            }

                            if (_toApply.Down)
                            {
                                entity_.State.Y -= 5.0f * 0.02f /*Time.fixedDeltaTime*/;
                            }

                            if (_toApply.Left)
                            {
                                entity_.State.X -= 5.0f * 0.02f /*Time.fixedDeltaTime*/;
                            }

                            if (_toApply.Right)
                            {
                                entity_.State.X += 5.0f * 0.02f /*Time.fixedDeltaTime*/;
                            }

                            //if (Connection.IsServer && toApply.Action)
                            //{
                            //  GameActionEvent evnt = Event.Create<GameActionEvent>(this);
                            //  evnt.Key = this.actionCount++;
                            //  this.Controller.QueueEvent(evnt, 2);
                            //}
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_PostUpdate += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_OnShutdown += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            entity_.Shutdown.Invoke();
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            entity_.Shutdown.Invoke();
                            break;
                        }
                    default:
                        break;
                }
            };

            manager.Event_Invoke += (@event, controller) =>
            {
                switch (@event)
                {
                    case GameActionEvent event_:
                        {
                            Client_OnGameActionEvent(event_);
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Event_InvokeEntity += (@event, controller, entity) =>
            {
                switch (@event)
                {
                    case GameActionEvent event_:
                        {
                            Client_OnGameActionEventEntity(event_, entity);
                            break;
                        }
                    default:
                        break;
                }
            };
        }

        private void Initialize()
        {
            var manager = ServiceProvider.GetRequiredService<ClientWorld>();
            manager.Entity_OnFrozen += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            entity_.Frozen.Invoke();
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            entity_.Frozen.Invoke();
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            entity_.Frozen.Invoke();
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_OnUnfrozen += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            entity_.Unfrozen.Invoke();
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            entity_.Unfrozen.Invoke();
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            entity_.Unfrozen.Invoke();
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_UpdateProxy += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_UpdateControlGeneric += (entity, command) =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            var _toPopulate = (GameCommand)command;
                            _toPopulate.SetData(entity_.Up, entity_.Down, entity_.Left, entity_.Right, entity_.Action);
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
            manager.Entity_Revert += entity =>
            {
                switch (entity)
                {
                    case ClientControlledEntity entity_:
                        {
                            break;
                        }
                    case ClientDummyEntity entity_:
                        {
                            break;
                        }
                    case ClientMimicEntity entity_:
                        {
                            break;
                        }
                    default:
                        break;
                }
            };
        }
    }
}
