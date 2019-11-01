namespace Papagei
{
    public abstract class ServerEntity : Entity, IPoolable<ServerEntity>
    {
        public ServerController Controller { get; set; }

        // #region Pooling
        public IPool<ServerEntity> Pool { get; set; }

        public virtual void Reset()
        {
            ResetCore();

            Controller = null;

            OutgoingStates.Clear();
            IncomingCommands.Clear();
        }
        // #endregion

        // We use no divisor for storing commands because commands are sent in
        // batches that we can use to fill in the holes between send ticks
        public DejitterBuffer<Command> IncomingCommands { get; } = new DejitterBuffer<Command>(Config.DEJITTER_BUFFER_LENGTH);
        public QueueBuffer<StateRecord> OutgoingStates { get; } = new QueueBuffer<StateRecord>(Config.DEJITTER_BUFFER_LENGTH);

        // The remote (client) tick of the last command we processed
        public Tick CommandAck { get; set; }
    }
}
