namespace Papagei
{
    public interface IScopeEvaluator
    {
        bool Evaluate(Event evnt);
        bool Evaluate(Entity entity, int ticksSinceSend, out float priority);
    }

    public class DefaultScopeEvaluator : IScopeEvaluator
    {
        public bool Evaluate(Event evnt)
        {
            return true;
        }

        public bool Evaluate(Entity entity, int ticksSinceSend, out float priority)
        {
            priority = 0.0f;
            return true;
        }
    }
}
