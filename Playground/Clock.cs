using System;
using System.Diagnostics;

namespace Playground
{
    internal class Clock
    {
        private readonly double _updateFrequency;
        public readonly long _start = Stopwatch.GetTimestamp();
        private readonly double _frequency = 1.0 / Stopwatch.Frequency;

        public Clock(double updateFrequency)
        {
            _updateFrequency = updateFrequency;
        }

        /// <summary>
        /// Time represented as elapsed seconds.
        /// </summary>
        public double Time => (Stopwatch.GetTimestamp() - _start) * _frequency; // diff * frequency

        public event Action OnFixedUpdate = () => { };

        private double lastUpdate;

        public void Start()
        {
            lastUpdate = Time;
        }

        public void Tick()
        {
            while ((lastUpdate + _updateFrequency) < Time)
            {
                OnFixedUpdate.Invoke();
                lastUpdate += _updateFrequency;
            }
        }
    }
}
