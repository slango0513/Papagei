using System.Diagnostics;

namespace Papagei.Client
{
    public class BitBufferClientPacketProtocol : IClientPacketProtocol
    {
        private readonly BitBuffer _buffer = new BitBuffer();
        private readonly ClientPools _pools;
        private readonly byte[] _bytes = new byte[Config.DATA_BUFFER_SIZE];

        public BitBufferClientPacketProtocol(ClientPools pools)
        {
            _pools = pools;
        }

        public ClientIncomingPacket Decode(byte[] data, int length)
        {
            var reusableIncoming = new ClientIncomingPacket();
            _buffer.Load(data, length);
            _buffer.DecodePacket(reusableIncoming, _pools.EventTypeCompressor, typeCode => _pools.CreateEvent(typeCode), (buffer, packet) =>
            {
                // Read: [Deltas]
                buffer.Decode(packet.ReceivedDeltas, () =>
                {
                    //var delta = buffer.DecodeDelta(packet.SenderTick);
                    var packetTick = packet.SenderTick;

                    var delta = _pools.DeltaPool.Allocate();
                    State state = null;

                    // Read: [EntityId]
                    var entityId = buffer.ReadEntityId();

                    // Read: [IsFrozen]
                    var isFrozen = buffer.ReadBool();

                    if (isFrozen == false)
                    {
                        // Read: [FactoryType]
                        var typeCode = buffer.ReadInt(_pools.EntityTypeCompressor);
                        state = _pools.CreateState(typeCode);

                        // Read: [IsRemoved]
                        var isRemoved = buffer.ReadBool();

                        if (isRemoved)
                        {
                            // Read: [DestroyedTick]
                            state.RemovedTick = buffer.ReadTick();

                            // End Read
                        }
                        else
                        {
                            // Read: [HasControllerData]
                            state.HasControllerData = buffer.ReadBool();

                            // Read: [HasImmutableData]
                            state.HasImmutableData = buffer.ReadBool();

                            // Read: [Flags]
                            state.Flags = buffer.Read(state.FlagBits);

                            // Read: [Mutable Data]
                            state.DecodeMutableData(buffer, state.Flags);

                            if (state.HasControllerData)
                            {
                                // Read: [Controller Data]
                                state.DecodeControllerData(buffer);

                                // Read: [Command Ack]
                                state.CommandAck = buffer.ReadTick();
                            }

                            if (state.HasImmutableData)
                            {
                                // Read: [Immutable Data]
                                state.DecodeImmutableData(buffer);
                            }
                        }
                    }

                    delta.Initialize(packetTick, entityId, state, isFrozen);
                    return delta;
                });
            });
            if (_buffer.IsFinished)
            {
                return reusableIncoming;
            }
            else
            {
                return default;
            }
        }

        public (byte[], int) Encode(ClientOutgoingPacket packet_)
        {
            // Send the packet
            _buffer.Clear();
            _buffer.EncodePacket(packet_, _pools.EventTypeCompressor, (buffer, reservedBytes, packet) =>
            {
                // Write: [Commands]
                buffer.Encode(packet.PendingCommandUpdates, packet.SentCommandUpdates, Config.PACKCAP_COMMANDS, Config.MAXSIZE_COMMANDUPDATE, update =>
                {
                    //buffer.Encode_CommandUpdate(commandUpdate);
                    // Write: [EntityId]
                    buffer.WriteEntityId(update.EntityId);

                    // Write: [Count]
                    buffer.Write(CommandUpdate.BUFFER_COUNT_BITS, (uint)update.Commands.Count);

                    // Write: [Commands]
                    foreach (var command in update.Commands.GetValues())
                    {
                        //buffer.Encode_Command(command);
                        // Write: [SenderTick]
                        buffer.WriteTick(command.ClientTick);

                        // Write: [Command Data]
                        command.EncodeData(buffer);
                    }
                });

                // Write: [View]
                //var ordered = view.GetOrdered();
                buffer.PackToSize(Config.PACKCAP_MESSAGE_TOTAL - reservedBytes, int.MaxValue, packet.View.SortList, (pair) =>
                {
                    buffer.WriteEntityId(pair.Key); // Write: [EntityId]
                    buffer.WriteTick(pair.Value.Tick); // Write: [Tick]
                    buffer.WriteBool(pair.Value.IsFrozen); // Write: [IsFrozen]
                });
            });
            var length = _buffer.Store(_bytes);
            Debug.Assert(length <= Config.PACKCAP_MESSAGE_TOTAL);
            return (_bytes, length);
        }
    }
}
