using System.Collections.Generic;
using System.Diagnostics;

namespace Papagei
{
    public abstract class BitBufferServerPacketProtocol : IServerPacketProtocol
    {
        protected readonly BitBuffer _buffer = new BitBuffer();
        private readonly ServerPools _pools;
        private readonly byte[] _bytes = new byte[Config.DATA_BUFFER_SIZE];

        public BitBufferServerPacketProtocol(ServerPools pools)
        {
            _pools = pools;
        }

        public ServerIncomingPacket Decode(byte[] data, int length)
        {
            var reusableIncoming = new ServerIncomingPacket();
            _buffer.Load(data, length);
            _buffer.DecodePacket(reusableIncoming, _pools.EventTypeCompressor, typeCode => _pools.CreateEvent(typeCode), (buffer, packet) =>
            {
                // Read: [Commands]
                buffer.Decode(packet.ReceivedCommandUpdates, () =>
                {
                    //var update = buffer.Decode_CommandUpdate();
                    var update = _pools.CommandUpdatePool.Allocate();

                    // Read: [EntityId]
                    update.EntityId = buffer.ReadEntityId();

                    // Read: [Count]
                    var count = (int)buffer.Read(CommandUpdate.BUFFER_COUNT_BITS);

                    // Read: [Commands]
                    for (int i = 0; i < count; i++)
                    {
                        //var command = buffer.Decode_Command();
                        var command = _pools.CommandPool.Allocate();

                        // Read: [SenderTick]
                        command.ClientTick = buffer.ReadTick();

                        // Read: [Command Data]
                        DecodeData(command);

                        update.Commands.Store(command);
                    }
                    return update;
                });

                // Read: [View]
                var decoded = buffer.UnpackAll(() =>
                {
                    // Read: [EntityId], Read: [Tick], Read: [IsFrozen]
                    return new KeyValuePair<EntityId, ViewEntry>(buffer.ReadEntityId(), new ViewEntry(buffer.ReadTick(), buffer.ReadBool()));
                });

                foreach (var pair in decoded)
                {
                    packet.View.RecordUpdate(pair.Key, pair.Value);
                }
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

        public (byte[], int) Encode(ServerOutgoingPacket packet_)
        {
            _buffer.Clear();
            _buffer.EncodePacket(packet_, _pools.EventTypeCompressor, (buffer, reservedBytes, packet) =>
            {
                // Write: [Deltas]
                // EncodeDeltas
                buffer.Encode(packet.PendingDeltas, packet.SentDeltas, Config.PACKCAP_MESSAGE_TOTAL - reservedBytes, Config.MAXSIZE_ENTITY, (delta) =>
                {
                    //buffer.EncodeDelta(delta);
                    // Write: [EntityId]
                    buffer.WriteEntityId(delta.EntityId);

                    // Write: [IsFrozen]
                    buffer.WriteBool(delta.IsFrozen);

                    if (!delta.IsFrozen)
                    {
                        // Write: [FactoryType]
                        var state = delta.State;
                        buffer.WriteInt(_pools.EntityTypeCompressor, state.TypeCode);

                        // Write: [IsRemoved]
                        buffer.WriteBool(state.RemovedTick.IsValid);

                        if (state.RemovedTick.IsValid)
                        {
                            // Write: [RemovedTick]
                            buffer.WriteTick(state.RemovedTick);

                            // End Write
                        }
                        else
                        {
                            // Write: [HasControllerData]
                            buffer.WriteBool(state.HasControllerData);

                            // Write: [HasImmutableData]
                            buffer.WriteBool(state.HasImmutableData);

                            // Write: [Flags]
                            buffer.Write(state.FlagBits, state.Flags);

                            // Write: [Mutable Data]
                            EncodeMutableData(state.Flags, state);

                            if (state.HasControllerData)
                            {
                                // Write: [Controller Data]
                                EncodeControllerData(state);

                                // Write: [Command Ack]
                                buffer.WriteTick(state.CommandAck);
                            }

                            if (state.HasImmutableData)
                            {
                                // Write: [Immutable Data]
                                EncodeImmutableData(state);
                            }
                        }
                    }
                });
            });
            var length = _buffer.Store(_bytes);
            Debug.Assert(length <= Config.PACKCAP_MESSAGE_TOTAL);
            return (_bytes, length);
        }

        public abstract void EncodeMutableData(uint flags, State state);
        public abstract void EncodeControllerData(State state);
        public abstract void EncodeImmutableData(State state);

        public abstract void DecodeData(Command command);
    }
}
