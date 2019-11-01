using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    /// <summary>
    /// A peer created by the server representing a connected client.
    /// </summary>
    public class ServerController : Controller
    {
        /// <summary>
        /// The entities controlled by this controller.
        /// </summary>
        public HashSet<ServerEntity> ControlledEntities { get; } = new HashSet<ServerEntity>();

        /// <summary>
        /// Used for setting the scope evaluator heuristics.
        /// </summary>
        //public ScopeEvaluator ScopeEvaluator { set => Scope.Evaluator = value; }

        public Scope Scope { get; } = new Scope();

        internal ServerController(IConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Adds an entity to be controlled by this peer.
        /// </summary>
        public void GrantControl(ServerEntity entity)
        {
            if (entity.Controller != this)
            {
                Debug.Assert(entity.Controller == null);
                ControlledEntities.Add(entity);

                entity.Controller = this;
                entity.IncomingCommands.Clear();
                entity.DeferNotifyControllerChanged = true;
            }
        }

        /// <summary>
        /// Remove an entity from being controlled by this peer.
        /// </summary>
        public void RevokeControl(ServerEntity entity)
        {
            Debug.Assert(entity.Controller == this);
            ControlledEntities.Remove(entity);

            entity.Controller = null;
            entity.IncomingCommands.Clear();
            entity.DeferNotifyControllerChanged = true;
        }
    }
}
