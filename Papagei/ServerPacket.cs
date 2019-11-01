using MessagePack;
using System.Collections.Generic;

namespace Papagei
{
    public interface IServerPacketProtocol
    {
        ServerIncomingPacket Decode(byte[] data, int length);
        (byte[], int) Encode(ServerOutgoingPacket packet);
    }

    [MessagePackObject]
    public class ServerIncomingPacket : Packet
    {
        [Key(6)]
        public List<ServerCommandUpdate> ReceivedCommandUpdates { get; } = new List<ServerCommandUpdate>();
        [Key(7)]
        public View View { get; } = new View();

        public override void Reset()
        {
            base.Reset();

            View.LatestUpdates.Clear();
            ReceivedCommandUpdates.Clear();
        }
    }

    [MessagePackObject]
    public class ServerOutgoingPacket : Packet
    {
        [Key(6)]
        public List<StateDelta> PendingDeltas { get; } = new List<StateDelta>();
        [Key(7)]
        public List<StateDelta> SentDeltas { get; } = new List<StateDelta>();

        public override void Reset()
        {
            base.Reset();

            //Deltas.Clear();
            // Everything in sent is also in pending, so only free pending
            foreach (var value in PendingDeltas)
            {
                value.Pool.Deallocate(value);
            }

            PendingDeltas.Clear();
            SentDeltas.Clear();
        }
    }
}
