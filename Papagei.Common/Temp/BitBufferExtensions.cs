using System;
using System.Collections.Generic;

namespace Papagei
{
    public static class BitBufferExtensions
    {
        // Tick
        public static void WriteTick(this BitBuffer buffer, Tick tick)
        {
            buffer.WriteUInt(tick.TickValue);
        }

        public static Tick ReadTick(this BitBuffer buffer)
        {
            return new Tick(buffer.ReadUInt());
        }

        public static Tick PeekTick(this BitBuffer buffer)
        {
            return new Tick(buffer.PeekUInt());
        }

        // SequenceId
        public static void WriteSequenceId(this BitBuffer buffer, SequenceId sequenceId)
        {
            buffer.Write(SequenceId.BITS_USED, sequenceId.RawValue);
        }

        public static SequenceId ReadSequenceId(this BitBuffer buffer)
        {
            return new SequenceId(buffer.Read(SequenceId.BITS_USED));
        }

        public static SequenceId PeekSequenceId(this BitBuffer buffer)
        {
            return new SequenceId(buffer.Peek(SequenceId.BITS_USED));
        }

        // EntityId
        public static void WriteEntityId(this BitBuffer buffer, EntityId entityId)
        {
            buffer.WriteUInt(entityId.IdValue);
        }

        public static EntityId ReadEntityId(this BitBuffer buffer)
        {
            return new EntityId(buffer.ReadUInt());
        }

        public static EntityId PeekEntityId(this BitBuffer buffer)
        {
            return new EntityId(buffer.PeekUInt());
        }
    }

    public static class CompressorBitBufferExtensions
    {
        // IntCompressor
        public static void WriteInt(this BitBuffer buffer, Int32Compressor compressor, int value)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                buffer.WriteUInt(compressor.Pack(value));
            }
            else
            {
                buffer.Write(compressor.RequiredBits, compressor.Pack(value));
            }
        }

        public static int ReadInt(this BitBuffer buffer, Int32Compressor compressor)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.ReadUInt());
            }
            else
            {
                return compressor.Unpack(buffer.Read(compressor.RequiredBits));
            }
        }

        public static int PeekInt(this BitBuffer buffer, Int32Compressor compressor)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.PeekUInt());
            }
            else
            {
                return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
            }
        }

        // FloatCompressor
        public static void WriteFloat(this BitBuffer buffer, SingleCompressor compressor, float value)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                buffer.WriteUInt(compressor.Pack(value));
            }
            else
            {
                buffer.Write(compressor.RequiredBits, compressor.Pack(value));
            }
        }

        public static float ReadFloat(this BitBuffer buffer, SingleCompressor compressor)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.ReadUInt());
            }
            else
            {
                return compressor.Unpack(buffer.Read(compressor.RequiredBits));
            }
        }

        public static float PeekFloat(this BitBuffer buffer, SingleCompressor compressor)
        {
            if (compressor.RequiredBits > Config.VARINT_FALLBACK_SIZE)
            {
                return compressor.Unpack(buffer.PeekUInt());
            }
            else
            {
                return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
            }
        }
    }

    public static class PackedListBitBufferExtensions
    {
        public static void Decode<T>(this BitBuffer buffer, List<T> receivedList, Func<T> decode) where T : IPoolable<T>
        {
            var decoded = buffer.UnpackAll(decode);
            foreach (var delta in decoded)
            {
                receivedList.Add(delta);
            }
        }

        public static void Encode<T>(this BitBuffer buffer, List<T> pendingList, List<T> sentList, int maxTotalSize, int maxIndividualSize, Action<T> encode) where T : IPoolable<T>
        {
            buffer.PackToSize(maxTotalSize, maxIndividualSize, pendingList, encode, (val) => sentList.Add(val));
        }
    }

    public static class PacketBitBufferExtensions
    {
        /// <summary>
        /// After writing the header we write the packet data in three passes.
        /// The first pass is a fill of events up to a percentage of the packet.
        /// The second pass is the payload value, which will try to fill the
        /// remaining packet space. If more space is available, we will try
        /// to fill it with any remaining events, up to the maximum packet size.
        /// </summary>
        public static void EncodePacket<T>(this BitBuffer buffer, T packet, Int32Compressor eventTypeCompressor, Action<BitBuffer, int, T> onEncodePayload) where T : Packet
        {
            // Write: [Header]
            {
                // Write: [LocalTick]
                buffer.WriteTick(packet.SenderTick);

                // Write: [AckTick]
                buffer.WriteTick(packet.AckTick);

                // Write: [AckReliableEventId]
                buffer.WriteSequenceId(packet.AckEventId);
            }

            // Write: [Events] (Early Pack)
            EncodeEvents(Config.PACKCAP_EARLY_EVENTS);

            // Write: [Payload]
            //packet.EncodePayload(buffer, 1); // Leave one byte for the event count
            onEncodePayload(buffer, 1, packet);

            // Write: [Events] (Fill Pack)
            EncodeEvents(Config.PACKCAP_MESSAGE_TOTAL);

            /// <summary>
            /// Writes as many events as possible up to maxSize and returns the number
            /// of events written in the batch. Also increments the total counter.
            /// </summary>
            void EncodeEvents(int maxSize)
            {
                packet.EventsWritten += buffer.PackToSize(maxSize, Config.MAXSIZE_EVENT, GetNextEvents(),
                    evnt =>
                    {
                        /// <summary>
                        /// Note that the packetTick may not be the tick this event was created on
                        /// if we're re-trying to send this event in subsequent packets. This tick
                        /// is intended for use in tick diffs for compression.
                        /// </summary>
                        //Event.Encode
                        var packetTick = packet.SenderTick;

                        // Write: [EventType]
                        buffer.WriteInt(eventTypeCompressor, evnt.TypeCode);

                        // Write: [EventId]
                        buffer.WriteSequenceId(evnt.EventId);

                        // Write: [HasEntityId]
                        buffer.WriteBool(evnt.EntityId.IsValid);

                        if (evnt.EntityId.IsValid)
                        {
                            // Write: [EntityId]
                            buffer.WriteEntityId(evnt.EntityId);
                        }

                        // Write: [EventData]
                        evnt.EncodeData(buffer, packetTick);
                    },
                    evnt =>
                    {
                        // Event.RegisterSent
                        if (evnt.Attempts > 0)
                        {
                            evnt.Attempts--;
                        }
                    });

                IEnumerable<Event> GetNextEvents()
                {
                    for (int i = packet.EventsWritten; i < packet.PendingEvents.Count; i++)
                    {
                        yield return packet.PendingEvents[i];
                    }
                }
            }
        }

        public static void DecodePacket<T>(this BitBuffer buffer, T packet, Int32Compressor eventTypeCompressor, Func<int, Event> eventFactory, Action<BitBuffer, T> onDecodePayload) where T : Packet
        {
            // Read: [Header]
            {
                // Read: [LocalTick]
                packet.SenderTick = buffer.ReadTick();

                // Read: [AckTick]
                packet.AckTick = buffer.ReadTick();

                // Read: [AckReliableEventId]
                packet.AckEventId = buffer.ReadSequenceId();
            }

            // Read: [Events] (Early Pack)
            DecodeEvents();

            // Read: [Payload]
            //packet.DecodePayload(buffer);
            onDecodePayload(buffer, packet);

            // Read: [Events] (Fill Pack)
            DecodeEvents();

            void DecodeEvents()
            {
                var decoded = buffer.UnpackAll(() =>
                {
                    var packetTick = packet.SenderTick;
                    {
                        /// <summary>
                        /// Note that the packetTick may not be the tick this event was created on
                        /// if we're re-trying to send this event in subsequent packets. This tick
                        /// is intended for use in tick diffs for compression.
                        /// </summary>
                        // Event Decode
                        // Read: [EventType]
                        var typeCode = buffer.ReadInt(eventTypeCompressor);

                        var evnt = eventFactory(typeCode);

                        // Read: [EventId]
                        evnt.EventId = buffer.ReadSequenceId();

                        // Read: [HasEntityId]
                        var hasEntityId = buffer.ReadBool();

                        if (hasEntityId)
                        {
                            // Read: [EntityId]
                            evnt.EntityId = buffer.ReadEntityId();
                        }

                        // Read: [EventData]
                        evnt.DecodeData(buffer, packetTick);

                        return evnt;
                    }
                });
                foreach (var evnt in decoded)
                {
                    packet.Events.Add(evnt);
                }
            }
        }
    }
}
