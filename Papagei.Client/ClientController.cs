using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei.Client
{
    /// <summary>
    /// The peer created by the client representing the server.
    /// </summary>
    public class ClientController : Controller
    {
        /// <summary>
        /// The entities controlled by this controller.
        /// </summary>
        public HashSet<ClientEntity> ControlledEntities { get; } = new HashSet<ClientEntity>();

        internal event Action<ClientIncomingPacket> PacketReceived = (packet) => { };

        public View LocalView { get; } = new View();

        public List<ClientEntity> SortingList { get; } = new List<ClientEntity>();

        internal ClientController(IConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Adds an entity to be controlled by this peer.
        /// </summary>
        public void GrantControl(ClientEntity entity)
        {
            if (entity.Controller != this)
            {
                Debug.Assert(entity.Controller == null);
                ControlledEntities.Add(entity);

                entity.Controller = this;
                entity.OutgoingCommands.Clear();
                entity.DeferNotifyControllerChanged = true;
            }
        }

        /// <summary>
        /// Remove an entity from being controlled by this peer.
        /// </summary>
        public void RevokeControl(ClientEntity entity)
        {
            Debug.Assert(entity.Controller == this);
            ControlledEntities.Remove(entity);

            entity.Controller = null;
            entity.OutgoingCommands.Clear();
            entity.DeferNotifyControllerChanged = true;
        }
    }
}
