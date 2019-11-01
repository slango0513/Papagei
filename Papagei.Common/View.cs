using MessagePack;
using System.Collections.Generic;

namespace Papagei
{
    [MessagePackObject]
    public struct ViewEntry
    {
        internal static readonly ViewEntry INVALID = new ViewEntry(Tick.INVALID, true);

        [Key(0)]
        public Tick Tick { get; }
        [Key(1)]
        public bool IsFrozen { get; }

        public ViewEntry(Tick tick, bool isFrozen)
        {
            Tick = tick;
            IsFrozen = isFrozen;
        }
    }

    public class ViewComparer : Comparer<KeyValuePair<EntityId, ViewEntry>>
    {
        private readonly Comparer<Tick> _comparer = new TickComparer();

        public override int Compare(KeyValuePair<EntityId, ViewEntry> x, KeyValuePair<EntityId, ViewEntry> y)
        {
            return _comparer.Compare(x.Value.Tick, y.Value.Tick);
        }
    }

    [MessagePackObject]
    public class View
    {
        [Key(0)]
        public Dictionary<EntityId, ViewEntry> LatestUpdates { get; } = new Dictionary<EntityId, ViewEntry>();

        [Key(1)]
        public List<KeyValuePair<EntityId, ViewEntry>> SortList { get; } = new List<KeyValuePair<EntityId, ViewEntry>>();

        /// <summary>
        /// Returns the latest tick the peer has acked for this entity ID.
        /// </summary>
        public ViewEntry GetLatest(EntityId id)
        {
            return LatestUpdates.TryGetValue(id, out var result) ? result : ViewEntry.INVALID;
        }

        /// <summary>
        /// Records an acked status from the peer for a given entity ID.
        /// </summary>
        public void RecordUpdate(EntityId entityId, ViewEntry entry)
        {
            if (!LatestUpdates.TryGetValue(entityId, out var currentEntry) || currentEntry.Tick <= entry.Tick)
            {
                LatestUpdates[entityId] = entry;
            }
        }
    }
}
