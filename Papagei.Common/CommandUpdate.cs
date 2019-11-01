using MessagePack;

namespace Papagei
{
    public abstract class CommandUpdate
    {
        private const int BUFFER_CAPACITY = Config.COMMAND_SEND_COUNT;
        public static readonly int BUFFER_COUNT_BITS = Util.Log2(BUFFER_CAPACITY) + 1;

        protected void ResetCore()
        {
            EntityId = EntityId.INVALID;
            Commands.Clear();
        }

        [Key(0)]
        public EntityId EntityId { get; set; } = EntityId.INVALID;
        [Key(1)]
        public RollingBuffer<Command> Commands { get; } = new RollingBuffer<Command>(BUFFER_CAPACITY);
    }
}
