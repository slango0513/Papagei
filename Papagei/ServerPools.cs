using System;
using System.Collections.Generic;

namespace Papagei
{
    public class ServerPools : Pools
    {
        public Dictionary<int, IPool<ServerEntity>> EntityFactories { get; } = new Dictionary<int, IPool<ServerEntity>>();
        public Int32Compressor EntityTypeCompressor { get; }

        public IPool<ServerCommandUpdate> CommandUpdatePool { get; } = new Pool<ServerCommandUpdate>();
        public IPool<StateRecord> RecordPool { get; } = new Pool<StateRecord>();

        public ServerPools(Type commandType, IEnumerable<Type> eventTypes, IEnumerable<KeyValuePair<Type, Type>> entityTypes)
            : base(commandType, eventTypes)
        {
            // Register Entities
            foreach (var pair in entityTypes)
            {
                var entityType = pair.Key;
                var stateType = pair.Value;

                var statePool = CreatePool<State>(stateType);
                var entityPool = CreatePool<ServerEntity>(entityType);

                var typeKey = StatePools.Count + 1; // 0 is an invalid type
                StatePools.Add(typeKey, statePool);
                EntityFactories.Add(typeKey, entityPool);
                EntityTypeToKey.Add(entityType, typeKey);
            }

            EntityTypeCompressor = new Int32Compressor(0, EntityFactories.Count + 1);
        }
    }
}
