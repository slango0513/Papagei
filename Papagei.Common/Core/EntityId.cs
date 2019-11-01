using MessagePack;
using System.Collections.Generic;

namespace Papagei
{
    [MessagePackObject]
    public struct EntityId
    {
        public class EntityIdComparer : IEqualityComparer<EntityId>
        {
            public bool Equals(EntityId x, EntityId y)
            {
                return x.IdValue == y.IdValue;
            }

            public int GetHashCode(EntityId x)
            {
                return (int)x.IdValue;
            }
        }

        public static readonly EntityId INVALID = new EntityId(0);
        public static readonly EntityId START = new EntityId(1);
        public static readonly EntityIdComparer Comparer = new EntityIdComparer();

        [IgnoreMember]
        public bool IsValid => IdValue > 0;

        [Key(0)]
        public uint IdValue { get; }

        public EntityId(uint idValue)
        {
            IdValue = idValue;
        }

        public EntityId GetNext()
        {
            return new EntityId(IdValue + 1);
        }

        public override int GetHashCode()
        {
            return (int)IdValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is EntityId)
            {
                return ((EntityId)obj).IdValue == IdValue;
            }

            return false;
        }

        public override string ToString()
        {
            return "EntityId:" + IdValue;
        }
    }
}
