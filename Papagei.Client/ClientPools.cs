using System;
using System.Collections.Generic;

namespace Papagei.Client
{
    public class ClientPools : Pools
    {
        public Dictionary<int, IPool<ClientEntity>> EntityFactories { get; } = new Dictionary<int, IPool<ClientEntity>>();
        public Int32Compressor EntityTypeCompressor { get; }

        public IPool<ClientCommandUpdate> CommandUpdatePool { get; } = new Pool<ClientCommandUpdate>();

        public ClientPools(Type commandType, IEnumerable<Type> eventTypes, IEnumerable<KeyValuePair<Type, Type>> entityTypes)
            : base(commandType, eventTypes)
        {
            // Register Entities
            foreach (var pair in entityTypes)
            {
                var entityType = pair.Key;
                var stateType = pair.Value;

                var statePool = CreatePool<State>(stateType);
                var entityPool = CreatePool<ClientEntity>(entityType);

                var typeKey = StatePools.Count + 1; // 0 is an invalid type
                StatePools.Add(typeKey, statePool);
                EntityFactories.Add(typeKey, entityPool);
                EntityTypeToKey.Add(entityType, typeKey);
            }

            EntityTypeCompressor = new Int32Compressor(0, EntityFactories.Count + 1);
        }
    }
}
