using System.Collections.Generic;

namespace Papagei.Client
{
    public class ClientEntity : Entity, IPoolable<ClientEntity>
    {
        public ClientController Controller { get; set; }

        // #region Pooling
        public IPool<ClientEntity> Pool { get; set; }

        public virtual void Reset()
        {
            ResetCore();

            Controller = null;

            LastSentCommandTick = Tick.START;
            IsFrozen = true; // Entities start frozen on client
            ShouldBeFrozen = true;

            IncomingStates.Clear();
            {
                // DrainQueue
                while (OutgoingCommands.Count > 0)
                {
                    var val = OutgoingCommands.Dequeue();
                    val.Pool.Deallocate(val);
                }
            }

            AuthTick = Tick.START;
            NextTick = Tick.INVALID;

            {
                //ResetStates();
                if (AuthStateBase != null)
                {
                    AuthStateBase.Pool.Deallocate(AuthStateBase);
                }
                if (NextStateBase != null)
                {
                    NextStateBase.Pool.Deallocate(NextStateBase);
                }

                AuthStateBase = null;
                NextStateBase = null;
            }
        }
        // #endregion

        /// <summary>
        /// The tick of the last authoritative state.
        /// </summary>
        public Tick AuthTick { get; set; }

        /// <summary>
        /// The tick of the next authoritative state. May be invalid.
        /// </summary>
        public Tick NextTick { get; set; }

        public State AuthStateBase { get; set; }
        public State NextStateBase { get; set; }
        public Queue<Command> OutgoingCommands { get; } = new Queue<Command>();
        public Tick LastSentCommandTick { get; set; } // The last local tick we sent our commands to the server

        public DejitterBuffer<StateDelta> IncomingStates { get; } = new DejitterBuffer<StateDelta>(Config.DEJITTER_BUFFER_LENGTH, Config.NETWORK_SEND_RATE);
        public bool ShouldBeFrozen { get; set; }

        // Simulation info
        // Entities never freeze on server
        public bool IsFrozen { get; set; }
    }
}
