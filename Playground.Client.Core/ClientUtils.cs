using Papagei.Client;
using System.Numerics;

namespace Playground.Client
{
    public static class ClientUtils
    {
        public static bool DoSmoothing = false;

        // from DummyEntityBehaviour
        public static Vector2 GetSmoothedPosition(MyState current, MyState next)
        {
            if (current == default)
            {
                return default;
            }
            var curPos = new Vector2(current.X, current.Y);
            if (next == default)
            {
                return curPos;
            }
            var nextPos = new Vector2(next.X, next.Y);
            //float t = Entity.ComputeInterpolation(Time.fixedDeltaTime, Time.time - Time.fixedTime);
            var t = 0.5f;
            var pos = Lerp(curPos, nextPos, t);
            return pos;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return a * t + b * (1 - t);
        }

        // from Entity<TState>
        /// <summary>
        /// Returns the current dejittered authoritative state from the server.
        /// Will return null if the entity is locally controlled (use State).
        /// </summary>
        public static MyState GetAuthState(ClientEntity entity)
        {
            // Not valid if we're controlling
            if (entity.Controller == null)
            {
                return null;
            }

            return (MyState)entity.AuthStateBase;
        }

        /// <summary>
        /// Returns the next dejittered authoritative state from the server. Will 
        /// return null none is available or if the entity is locally controlled.
        /// </summary>
        public static MyState GetNextState(ClientEntity entity)
        {
            // Not valid if we're controlling
            if (entity.Controller == null)
            {
                return null;
            }
            // Only return if we have a valid next state assigned
            if (entity.NextTick.IsValid)
            {
                return (MyState)entity.NextStateBase;
            }

            return null;
        }
    }
}
