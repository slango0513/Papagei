namespace Papagei
{
    public static class Config
    {
        /// <summary>
        /// Network send rate in frames/packet.
        /// </summary>
        public const int NETWORK_SEND_RATE = 2;

        /// <summary>
        /// Number of outgoing commands to send per packet.
        /// </summary>
        internal const int COMMAND_SEND_COUNT = 8;

        /// <summary>
        /// Number of commands to buffer for prediction.
        /// </summary>
        public const int COMMAND_BUFFER_COUNT = 50;

        /// <summary>
        /// Number of entries to store in a dejitter buffer.
        /// </summary>
        public const int DEJITTER_BUFFER_LENGTH = 50;

        /// <summary>
        /// Number of ticks without data before freezing an entity.
        /// </summary>
        internal const int TICKS_BEFORE_FREEZE = 20;

        // #region Message Sizes
        /// <summary>
        /// Data buffer size used for packet I/O. 
        /// Don't change this without a good reason.
        /// </summary>
        public const int DATA_BUFFER_SIZE = 2048;

        /// <summary>
        /// The maximum message size that a packet can contain, based on known
        /// MTUs for internet traffic. Don't change this without a good reason.
        /// 
        /// If using MiniUDP, this should be equal to NetConfig.DATA_MAXIMUM
        /// </summary>
        public const int PACKCAP_MESSAGE_TOTAL = 1200;

        /// <summary>
        /// The max byte size when doing a first pass on packing events.
        /// </summary>
        internal const int PACKCAP_EARLY_EVENTS = 370;

        /// <summary>
        /// The max byte size when packing commands. (Client-only.)
        /// </summary>
        public const int PACKCAP_COMMANDS = 670;

        /// <summary>
        /// Maximum bytes for a single entity. Used when packing entity deltas.
        /// </summary>
        public const int MAXSIZE_ENTITY = 100;

        /// <summary>
        /// Maximum bytes for a single event. 
        /// </summary>
        internal const int MAXSIZE_EVENT = 100;

        /// <summary>
        /// Maximum bytes for a single command update.
        /// </summary>
        public const int MAXSIZE_COMMANDUPDATE = 100;

        /// <summary>
        /// Number of bits before doing VarInt fallback in compression.
        /// </summary>
        internal const int VARINT_FALLBACK_SIZE = 10;

        /// <summary>
        /// Maximum size for an encoded string.
        /// </summary>
        internal const int STRING_LENGTH_MAX = 63;
        // #endregion
    }
}
