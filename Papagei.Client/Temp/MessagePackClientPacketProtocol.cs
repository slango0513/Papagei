using MessagePack;
using MessagePack.Resolvers;

namespace Papagei.Client
{
    public class MessagePackClientPacketProtocol : IClientPacketProtocol
    {
        private readonly IFormatterResolver _resolver;

        public MessagePackClientPacketProtocol()
        {
            _resolver = StandardResolver.Instance;
        }

        private byte[] bytes = new byte[Config.DATA_BUFFER_SIZE];

        public ClientIncomingPacket Decode(byte[] data, int length)
        {
            return MessagePackSerializer.Deserialize<ClientIncomingPacket>(bytes, 0, _resolver, out var readSize);
        }

        public (byte[], int) Encode(ClientOutgoingPacket packet)
        {
            var writeSize = MessagePackSerializer.Serialize(ref bytes, 0, packet, _resolver);
            return (bytes, writeSize);
        }
    }
}
