using MessagePack;
using MessagePack.Resolvers;

namespace Papagei
{
    public class MessagePackServerPacketProtocol : IServerPacketProtocol
    {
        private readonly IFormatterResolver _resolver;

        public MessagePackServerPacketProtocol()
        {
            _resolver = StandardResolver.Instance;
        }

        private byte[] bytes = new byte[Config.DATA_BUFFER_SIZE];

        public ServerIncomingPacket Decode(byte[] data, int length)
        {
            return MessagePackSerializer.Deserialize<ServerIncomingPacket>(bytes, 0, _resolver, out var readSize);
        }

        public (byte[], int) Encode(ServerOutgoingPacket packet)
        {
            var writeSize = MessagePackSerializer.Serialize(ref bytes, 0, packet, _resolver);
            return (bytes, writeSize);
        }
    }
}
