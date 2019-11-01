using Papagei;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Playground
{
    public class ServerWorldHost
    {
        public IServiceProvider ServiceProvider { get; }

        public ServerWorldHost()
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
                [typeof(ServerControlledEntity)] = typeof(MyState),
                [typeof(ServerDummyEntity)] = typeof(DummyEntityState),
                [typeof(ServerMimicEntity)] = typeof(MyState),
            };
            var pools = new ServerPools(commandType, eventTypes, entityTypes);
            var manager = new ServerWorld(new BitBufferServerPacketProtocol(pools), pools);
            //var manager = new ServerWorld(new MessagePackServerPacketProtocol(), pools);
            services.AddSingleton(manager);

            ServiceProvider = services.BuildServiceProvider();

            InitializeCommon();
            Initialize();
        }

        private void InitializeCommon()
        {
            var manager = ServiceProvider.GetRequiredService<ServerWorld>();
            manager.Entity_OnStart += entity =>
            {
                switch (entity)
                {
                    case ServerControlledEntity entity_:
                        {
                            break;
                        }
                    case ServerDummyEntity entity_:
                        {
                            entity_.startX = entity_.State.X;
                            entity_.startY = entity_.State.Y;
                            entity_.startZ = entity_.State.Z;
                            entity_.angle = 0.0f;

                            entity_.distance = 1.0f + ((float)entity_.random.NextDouble() * 2.0f);
                            entity_.speed = 1.0f + ((float)entity_.random.NextDouble() * 2.0f);

                            if (entity_.random.NextDouble() > 0.5f)
                            {
                                entity_.speed *= -1.0f;
                            }
                            break;
                        }
                    case ServerMimicEntity entity_:
                        {
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
                    case ServerControlledEntity entity_:
                        {
                            break;
                        }
                    case ServerDummyEntity entity_:
                        {
                            break;
                        }
                    case ServerMimicEntity entity_:
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
                    case ServerControlledEntity entity_:
                        {
                            // 执行命令
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
                    case ServerDummyEntity entity_:
                        {
                            break;
                        }
                    case ServerMimicEntity entity_:
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
                    case ServerControlledEntity entity_:
                        {
                            break;
                        }
                    case ServerDummyEntity entity_:
                        {
                            break;
                        }
                    case ServerMimicEntity entity_:
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
                    case ServerControlledEntity entity_:
                        {
                            entity_.Shutdown.Invoke();
                            break;
                        }
                    case ServerDummyEntity entity_:
                        {
                            break;
                        }
                    case ServerMimicEntity entity_:
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
                            break;
                        }
                    default:
                        break;
                }
            };
        }

        private void Initialize()
        {
            var manager = ServiceProvider.GetRequiredService<ServerWorld>();
            manager.ControllerJoined += controller =>
            {
                var controlled = (ServerControlledEntity)manager.AddNewEntity(typeof(ServerControlledEntity));
                controlled.State.ArchetypeId = 0;
                controller.GrantControl(controlled);
                controller.Scope.Evaluator = new GameScopeEvaluator(controlled);
                controller.UserData = controlled;

                var mimic = (ServerMimicEntity)manager.AddNewEntity(typeof(ServerMimicEntity));
                mimic.State.ArchetypeId = 2;
                {
                    //mimic.Bind(controlled, 3.5f, 0.0f);
                    mimic.controlled = controlled;
                    mimic.xOffset = 3.5f;
                    mimic.yOffset = 0.0f;
                }
            };
            manager.ControllerLeft += controller =>
            {
                var controlled = (ServerControlledEntity)controller.UserData;
                manager.DestroyEntity(controlled);
            };

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var dummy = (ServerDummyEntity)manager.AddNewEntity(typeof(ServerDummyEntity));
                    dummy.State.ArchetypeId = 1;
                    dummy.State.X = i * 10f;
                    dummy.State.Y = j * 10f;
                    dummy.State.Z = 0f;
                }
            }

            manager.Entity_UpdateAuth += entity =>
            {
                switch (entity)
                {
                    case ServerControlledEntity entity_:
                        {
                            break;
                        }
                    case ServerDummyEntity entity_:
                        {
                            entity_.angle += GameMath.FIXED_DELTA_TIME * entity_.speed;

                            var adjustedX = entity_.startX + entity_.distance;
                            var adjustedY = entity_.startY;
                            var adjustedZ = entity_.startZ;

                            var newX = (float)(entity_.startX + (adjustedX - entity_.startX) * Math.Cos(entity_.angle) - (adjustedY - entity_.startY) * Math.Sin(entity_.angle));
                            var newY = (float)(entity_.startY + (adjustedX - entity_.startX) * Math.Sin(entity_.angle) + (adjustedY - entity_.startY) * Math.Cos(entity_.angle));
                            var newZ = (entity_.State.Z <= 2) ? entity_.State.Z + (float)entity_.random.NextDouble() * 0.02f : 0;

                            entity_.State.X = newX;
                            entity_.State.Y = newY;
                            entity_.State.Z = newZ;
                            break;
                        }
                    case ServerMimicEntity entity_:
                        {
                            entity_.State.X = entity_.controlled.State.X + entity_.xOffset;
                            entity_.State.Y = entity_.controlled.State.Y + entity_.yOffset;
                            break;
                        }
                    default:
                        break;
                }
            };
        }
    }
}
