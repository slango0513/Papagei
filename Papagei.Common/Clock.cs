namespace Papagei
{
    /// <summary>
    /// Used for keeping track of the remote peer's clock.
    /// </summary>
    public class Clock
    {
        public const int INVALID_TICK = -1;

        private const int DELAY_MIN = 3;
        private const int DELAY_MAX = 9;

        private readonly int _remoteRate;
        private readonly int _delayDesired;
        private readonly int _delayMin;
        private readonly int _delayMax;
        private bool shouldUpdateEstimate = false;

        public bool ShouldTick { get; private set; } = false;
        public Tick EstimatedRemote { get; private set; } = Tick.INVALID;
        public Tick LatestRemote { get; private set; } = Tick.INVALID;

        public Clock(int remoteSendRate = Config.NETWORK_SEND_RATE, int delayMin = DELAY_MIN, int delayMax = DELAY_MAX)
        {
            _remoteRate = remoteSendRate;

            _delayMin = delayMin;
            _delayMax = delayMax;

            _delayDesired = ((_delayMax - _delayMin) / 2) + _delayMin;
        }

        public void UpdateLatest(Tick latestTick)
        {
            if (LatestRemote.IsValid == false)
            {
                LatestRemote = latestTick;
            }

            if (EstimatedRemote.IsValid == false)
            {
                EstimatedRemote = Tick.Subtract(LatestRemote, _delayDesired);
            }

            if (latestTick > LatestRemote)
            {
                LatestRemote = latestTick;
                shouldUpdateEstimate = true;
                ShouldTick = true;
            }
        }

        /// <summary>
        /// Returns the number of frames we should simulate to sync up with
        /// the predicted remote peer clock, if any.
        /// </summary>
        // See http://www.gamedev.net/topic/652186-de-jitter-buffer-on-both-the-client-and-server/
        public void Update()
        {
            if (!ShouldTick)
            {
                return; // 0;
            }

            EstimatedRemote += 1;
            if (shouldUpdateEstimate == false)
            {
                return; // 1;
            }

            var delta = LatestRemote - EstimatedRemote;

            if (ShouldSnapTick(delta))
            {
                // Reset
                EstimatedRemote = LatestRemote - _delayDesired;
                return; // 0;
            }
            else if (delta > _delayMax)
            {
                // Jump 1
                EstimatedRemote += 1;
                return; // 2;
            }
            else if (delta < _delayMin)
            {
                // Stall 1
                EstimatedRemote -= 1;
                return; // 0;
            }
            shouldUpdateEstimate = false;
            return; // 1;
        }

        private bool ShouldSnapTick(float delta)
        {
            if (delta < (_delayMin - _remoteRate))
            {
                return true;
            }
            if (delta > (_delayMax + _remoteRate))
            {
                return true;
            }
            return false;
        }
    }
}
