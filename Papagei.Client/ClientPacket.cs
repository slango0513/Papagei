using MessagePack;
using System.Collections.Generic;

namespace Papagei.Client
{
    public interface IClientPacketProtocol
    {
        ClientIncomingPacket Decode(byte[] data, int length);
        (byte[], int) Encode(ClientOutgoingPacket packet);
    }

    [MessagePackObject]
    public class ClientIncomingPacket : Packet
    {
        [Key(6)]
        public List<StateDelta> ReceivedDeltas { get; } = new List<StateDelta>();

        public override void Reset()
        {
            base.Reset();

            ReceivedDeltas.Clear();
        }
    }

    [MessagePackObject]
    public class ClientOutgoingPacket : Packet
    {
        [Key(6)]
        public List<ClientCommandUpdate> PendingCommandUpdates { get; } = new List<ClientCommandUpdate>();
        [Key(7)]
        public List<ClientCommandUpdate> SentCommandUpdates { get; } = new List<ClientCommandUpdate>();
        [Key(8)]
        public View View { get; } = new View();

        public override void Reset()
        {
            base.Reset();

            View.LatestUpdates.Clear();

            //CommandUpdates.Clear();
            // Everything in sent is also in pending, so only free pending
            foreach (var value in PendingCommandUpdates)
            {
                value.Pool.Deallocate(value);
            }

            PendingCommandUpdates.Clear();
            SentCommandUpdates.Clear();
        }
    }
}
