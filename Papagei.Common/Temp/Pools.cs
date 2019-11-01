using System;
using System.Collections.Generic;

namespace Papagei
{
    public abstract class Pools
    {
        // Read-only data structures, don't need to be thread-local
        //public Dictionary<int, IPool<TEntity>> EntityFactories { get; } = new Dictionary<int, IPool<TEntity>>();
        public Dictionary<Type, int> EntityTypeToKey { get; } = new Dictionary<Type, int>();
        public Dictionary<Type, int> EventTypeToKey { get; } = new Dictionary<Type, int>();
        public Int32Compressor EventTypeCompressor { get; }

        // Mutable pools, need to be cloned per-thread
        public IPool<Command> CommandPool { get; }
        public Dictionary<int, IPool<State>> StatePools { get; } = new Dictionary<int, IPool<State>>();
        public Dictionary<int, IPool<Event>> EventPools { get; } = new Dictionary<int, IPool<Event>>();

        public IPool<StateDelta> DeltaPool { get; } = new Pool<StateDelta>();

        public Pools(Type commandType, IEnumerable<Type> eventTypes)
        {
            // Register Command
            CommandPool = CreatePool<Command>(commandType);

            // Register Events
            foreach (var eventType in eventTypes)
            {
                var statePool = CreatePool<Event>(eventType);

                var typeKey = EventPools.Count + 1; // 0 is an invalid type
                EventPools.Add(typeKey, statePool);
                EventTypeToKey.Add(eventType, typeKey);
            }

            EventTypeCompressor = new Int32Compressor(0, EventPools.Count + 1);
        }

        protected IPool<T> CreatePool<T>(Type derivedType) where T : IPoolable<T>
        {
            var factoryType = typeof(Pool<,>);
            var specific = factoryType.MakeGenericType(typeof(T), derivedType);
            var ci = specific.GetConstructor(Type.EmptyTypes);
            return (IPool<T>)ci.Invoke(new object[] { });
        }

        public State CreateState(int typeCode)
        {
            var state = StatePools[typeCode].Allocate();
            state.TypeCode = typeCode;
            //state.InitializeData();
            return state;
        }

        public Event CreateEvent(Type type)
        {
            var typeCode = EventTypeToKey[type];
            var evnt = EventPools[typeCode].Allocate();
            evnt.TypeCode = typeCode;
            return evnt;
        }

        public Event CreateEvent(int typeCode)
        {
            var evnt = EventPools[typeCode].Allocate();
            evnt.TypeCode = typeCode;
            return evnt;
        }
    }
}
