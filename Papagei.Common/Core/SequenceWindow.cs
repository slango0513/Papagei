using System.Diagnostics;

namespace Papagei
{
    public struct SequenceWindow
    {
        public const int HISTORY_LENGTH = BitArray64.LENGTH;

        public static bool AreInRange(SequenceId lowest, SequenceId highest)
        {
            return (highest - lowest) <= HISTORY_LENGTH;
        }

        private readonly BitArray64 historyArray;

        public SequenceId Latest { get; }
        public bool IsValid => Latest.IsValid;

        public SequenceWindow(SequenceId latest)
        {
            Debug.Assert(latest.IsValid);
            Latest = latest;
            historyArray = new BitArray64();
        }

        private SequenceWindow(SequenceId latest, BitArray64 history)
        {
            Debug.Assert(latest.IsValid);
            Latest = latest;
            historyArray = history;
        }

        public SequenceWindow Store(SequenceId value)
        {
            var latest = Latest;
            var historyArray = this.historyArray;

            int difference = Latest - value;
            if (difference > 0)
            {
                historyArray = this.historyArray.Store(difference - 1);
            }
            else
            {
                int offset = -difference;
                historyArray = (this.historyArray << offset).Store(offset - 1);
                latest = value;
            }

            return new SequenceWindow(latest, historyArray);
        }

        public bool Contains(SequenceId value)
        {
            int difference = Latest - value;
            if (difference == 0)
            {
                return true;
            }

            return historyArray.Contains(difference - 1);
        }

        public bool IsNewId(SequenceId id)
        {
            if (ValueTooOld(id))
            {
                return false;
            }

            if (Contains(id))
            {
                return false;
            }

            return true;
        }

        public bool ValueTooOld(SequenceId value)
        {
            return (Latest - value) > HISTORY_LENGTH;
        }
    }
}
