using System;

namespace Papagei.Client
{
    public static class ClientEntityUtils
    {
        public static float ComputeInterpolation(ClientEntity entity, World<ClientEntity> world, float tickDeltaTime, float timeSinceTick)
        {
            if (entity.NextTick == Tick.INVALID)
            {
                throw new InvalidOperationException("Next state is invalid");
            }

            var curTime = entity.AuthTick.ToTime(tickDeltaTime);
            var nextTime = entity.NextTick.ToTime(tickDeltaTime);
            var showTime = world.World_Tick.ToTime(tickDeltaTime) + timeSinceTick;

            var progress = showTime - curTime;
            var span = nextTime - curTime;
            if (span <= 0.0f)
            {
                return 0.0f;
            }

            return progress / span;
        }

        /// <summary>
        /// Returns the number of ticks ahead we are, for extrapolation.
        /// Note that this does not take client-side prediction into account.
        /// </summary>
        public static int GetTicksAhead(ClientEntity entity, World<ClientEntity> world)
        {
            return world.World_Tick - entity.AuthTick;
        }
    }
}
