using Papagei;

namespace Playground
{
    public class GameScopeEvaluator : IScopeEvaluator
    {
        private const float MAX_DIST_SQR = 10000.0f;

        private readonly ServerControlledEntity controlled;

        public GameScopeEvaluator(ServerControlledEntity controlled)
        {
            this.controlled = controlled;
        }

        public bool Evaluate(Event evnt)
        {
            return true;
        }

        public bool Evaluate(Entity entity, int ticksSinceSend, out float priority)
        {
            priority = 0.0f;
            if (entity == controlled)
            {
                return true;
            }

            if (entity is IHasPosition hasPosition)
            {
                var origin = controlled.Position;
                var query = hasPosition.Position;

                var distance = (origin - query).LengthSquared();
                if (distance > MAX_DIST_SQR)
                {
                    return false;
                }

                priority = distance / ticksSinceSend;
                return true;
            }

            return true;
        }
    }
}
