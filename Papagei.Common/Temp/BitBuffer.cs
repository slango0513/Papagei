using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Papagei
{
    /// <summary>
    /// A first-in-first-out (FIFO) bit encoding buffer.
    /// </summary>
    public partial class BitBuffer
    {
        private static int FindHighestBitPosition(byte data)
        {
            var shiftCount = 0;
            while (data > 0)
            {
                data >>= 1;
                shiftCount++;
            }
            return shiftCount;
        }

        private const int GROW_FACTOR = 2;
        private const int MIN_GROW = 1;
        private const int DEFAULT_CAPACITY = 8;

        /// <summary>
        /// The position of the next-to-be-read bit.
        /// </summary>
        private int readPos = 0;

        /// <summary>
        /// The position of the next-to-be-written bit.
        /// </summary>
        public int writePos = 0;

        /// <summary>
        /// Buffer of chunks for storing data.
        /// </summary>
        private uint[] chunks;

        /// <summary>
        /// Size the buffer will require in bytes.
        /// </summary>
        public int ByteSize => ((writePos - 1) >> 3) + 1;

        /// <summary>
        /// Returns true iff we have read everything off of the buffer.
        /// </summary>
        public bool IsFinished => writePos == readPos;

        /// <summary>
        /// Capacity is in data chunks: uint = 4 bytes
        /// </summary>
        public BitBuffer(int capacity = DEFAULT_CAPACITY)
        {
            chunks = new uint[capacity];
        }

        /// <summary>
        /// Clears the buffer (does not overwrite values, but doesn't need to).
        /// </summary>
        public void Clear()
        {
            readPos = 0;
            writePos = 0;
        }

        /// <summary>
        /// Takes the lower numBits from the value and stores them in the buffer.
        /// </summary>
        public void Write(int numBits, uint value)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException("Pushing negatve bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException("Pushing too many bits");
            }

            var index = writePos >> 5;
            var used = writePos & 0x0000001F;

            if ((index + 1) >= chunks.Length)
            {
                ExpandArray();
            }

            var chunkMask = (1UL << used) - 1;
            var scratch = chunks[index] & chunkMask;
            var result = scratch | ((ulong)value << used);

            chunks[index] = (uint)result;
            chunks[index + 1] = (uint)(result >> 32);

            writePos += numBits;
        }

        /// <summary>
        /// Reads the next numBits from the buffer.
        /// </summary>
        public uint Read(int numBits)
        {
            var result = Peek(numBits);
            readPos += numBits;
            return result;
        }

        /// <summary>
        /// Peeks at the next numBits from the buffer.
        /// </summary>
        public uint Peek(int numBits)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException("Pushing negative bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException("Pushing too many bits");
            }

            var index = readPos >> 5;
            var used = readPos & 0x0000001F;

            var chunkMask = ((1UL << numBits) - 1) << used;
            ulong scratch = chunks[index];
            if ((index + 1) < chunks.Length)
            {
                scratch |= (ulong)chunks[index + 1] << 32;
            }

            ulong result = (scratch & chunkMask) >> used;

            return (uint)result;
        }

        /// <summary>
        /// Copies the buffer to a byte buffer.
        /// </summary>
        public int Store(byte[] data)
        {
            // Write a sentinel bit to find our position and flash out bad data
            Write(1, 1);

            var numChunks = (writePos >> 5) + 1;
            Debug.Assert(data.Length >= numChunks * 4, "Buffer too small");

            for (int i = 0; i < numChunks; i++)
            {
                int dataIdx = i * 4;
                uint chunk = chunks[i];
                data[dataIdx] = (byte)chunk;
                data[dataIdx + 1] = (byte)(chunk >> 8);
                data[dataIdx + 2] = (byte)(chunk >> 16);
                data[dataIdx + 3] = (byte)(chunk >> 24);
            }

            return ByteSize;
        }

        /// <summary>
        /// Overwrites this buffer with an array of byte data.
        /// </summary>
        public void Load(byte[] data, int length)
        {
            var numChunks = (length / 4) + 1;
            if (chunks.Length < numChunks)
            {
                chunks = new uint[numChunks];
            }

            for (int i = 0; i < numChunks; i++)
            {
                var dataIdx = i * 4;
                var chunk = data[dataIdx] | (uint)data[dataIdx + 1] << 8 | (uint)data[dataIdx + 2] << 16 | (uint)data[dataIdx + 3] << 24;
                chunks[i] = chunk;
            }

            var positionInByte = FindHighestBitPosition(data[length - 1]);

            // Take one off the position to backtrack from the sentinel bit
            writePos = ((length - 1) * 8) + (positionInByte - 1);
            readPos = 0;
        }

        /// <summary>
        /// Inserts data at a given position. Reserve the space first by writing
        /// a given number of zero bits and storing the position.
        /// </summary>
        private void Insert(int position, int numBits, uint value)
        {
            if (numBits < 0)
            {
                throw new ArgumentOutOfRangeException("Pushing negatve bits");
            }

            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException("Pushing too many bits");
            }

            var index = position >> 5;
            var used = position & 0x0000001F;

            var valueMask = (1UL << numBits) - 1;
            var prepared = (value & valueMask) << used;
            var scratch = chunks[index] | (ulong)chunks[index + 1] << 32;
            var result = scratch | prepared;

            chunks[index] = (uint)result;
            chunks[index + 1] = (uint)(result >> 32);
        }

        private void ExpandArray()
        {
            var newCapacity = (chunks.Length * GROW_FACTOR) + MIN_GROW;

            var newChunks = new uint[newCapacity];
            Array.Copy(chunks, newChunks, chunks.Length);
            chunks = newChunks;
        }

        public override string ToString()
        {
            var raw = new StringBuilder();
            for (int i = chunks.Length - 1; i >= 0; i--)
            {
                raw.Append(Convert.ToString(chunks[i], 2).PadLeft(32, '0'));
            }

            var spaced = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                spaced.Append(raw[i]);
                if (((i + 1) % 8) == 0)
                {
                    spaced.Append(" ");
                }
            }

            return spaced.ToString();
        }
    }

    // Enumerables
    public partial class BitBuffer
    {
        /// <summary>
        /// Packs all elements of an enumerable.
        /// Max 255 elements.
        /// </summary>
        public byte PackAll<T>(IEnumerable<T> elements, Action<T> encode)
        {
            byte count = 0;

            // Reserve: [Count]
            var countPosition = writePos;
            Write(8, 0);

            // Write: [Elements]
            foreach (T val in elements)
            {
                if (count == 255)
                {
                    break;
                }

                encode.Invoke(val);
                count++;
            }

            // Deferred Write: [Count]
            Insert(countPosition, 8, count);
            return count;
        }

        /// <summary>
        /// Packs all elements of an enumerable up to a given size. 
        /// Max 255 elements.
        /// </summary>
        public byte PackToSize<T>(int maxTotalBytes, int maxIndividualBytes, IEnumerable<T> elements, Action<T> encode, Action<T> packed = null)
        {
            const int MAX_SIZE = 255;
            const int SIZE_BITS = 8;

            maxTotalBytes -= 1; // Sentinel bit can blow this up
            byte count = 0;

            // Reserve: [Count]
            var countWritePos = writePos;
            Write(SIZE_BITS, 0);

            // Write: [Elements]
            foreach (var val in elements)
            {
                if (count == MAX_SIZE)
                {
                    break;
                }

                var rollback = writePos;
                var startByteSize = ByteSize;

                encode.Invoke(val);

                var endByteSize = ByteSize;
                var writeByteSize = endByteSize - startByteSize;
                if (writeByteSize > maxIndividualBytes)
                {
                    writePos = rollback;
                    Console.WriteLine($"Skipping {val} ({writeByteSize}B)");
                }
                else if (endByteSize > maxTotalBytes)
                {
                    writePos = rollback;
                    break;
                }
                else
                {
                    if (packed != null)
                    {
                        packed.Invoke(val);
                    }

                    count++;
                }
            }

            // Deferred Write: [Count]
            Insert(countWritePos, SIZE_BITS, count);
            return count;
        }

        /// <summary>
        /// Decodes all packed items. 
        /// Max 255 elements.
        /// </summary>
        public IEnumerable<T> UnpackAll<T>(Func<T> decode)
        {
            // Read: [Count]
            var count = ReadByte();

            // Read: [Elements]
            for (uint i = 0; i < count; i++)
            {
                yield return decode.Invoke();
            }
        }
    }

    // Byte
    public partial class BitBuffer
    {
        public void WriteByte(byte val)
        {
            Write(8, val);
        }

        public byte ReadByte()
        {
            return (byte)Read(8);
        }

        public byte PeekByte()
        {
            return (byte)Peek(8);
        }
    }

    // UInt
    public partial class BitBuffer
    {
        /// <summary>
        /// Writes using an elastic number of bytes based on number size:
        /// 
        ///    Bits   Min Dec    Max Dec     Max Hex     Bytes Used
        ///    0-7    0          127         0x0000007F  1 byte
        ///    8-14   128        1023        0x00003FFF  2 bytes
        ///    15-21  1024       2097151     0x001FFFFF  3 bytes
        ///    22-28  2097152    268435455   0x0FFFFFFF  4 bytes
        ///    28-32  268435456  4294967295  0xFFFFFFFF  5 bytes
        /// 
        /// </summary>
        public void WriteUInt(uint val)
        {
            var buffer = 0x0u;

            do
            {
                // Take the lowest 7 bits
                buffer = val & 0x7Fu;
                val >>= 7;

                // If there is more data, set the 8th bit to true
                if (val > 0)
                {
                    buffer |= 0x80u;
                }

                // Store the next byte
                Write(8, buffer);
            }
            while (val > 0);
        }

        public uint ReadUInt()
        {
            var buffer = 0x0u;
            var val = 0x0u;
            var s = 0;

            do
            {
                buffer = Read(8);

                // Add back in the shifted 7 bits
                val |= (buffer & 0x7Fu) << s;
                s += 7;

                // Continue if we're flagged for more
            } while ((buffer & 0x80u) > 0);

            return val;
        }

        public uint PeekUInt()
        {
            var tempPosition = readPos;
            var val = ReadUInt();
            readPos = tempPosition;
            return val;
        }
    }

    // Int
    public partial class BitBuffer
    {
        public void WriteInt(int val)
        {
            var zigzag = (uint)((val << 1) ^ (val >> 31));
            WriteUInt(zigzag);
        }

        public int ReadInt()
        {
            var val = ReadUInt();
            var zagzig = (int)((val >> 1) ^ (-(val & 1)));
            return zagzig;
        }

        public int PeekInt()
        {
            var val = PeekUInt();
            var zagzig = (int)((val >> 1) ^ (-(val & 1)));
            return zagzig;
        }
    }

    // Bool
    public partial class BitBuffer
    {
        public void WriteBool(bool value)
        {
            Write(1, value ? 1U : 0U);
        }

        public bool ReadBool()
        {
            return Read(1) > 0;
        }

        public bool PeekBool()
        {
            return Peek(1) > 0;
        }
    }

    // String
    public partial class BitBuffer
    {
        // 7 bits for 0-127 on the simple ASCII table
        private const int ASCII_BITS = 7;
        private static readonly int STRING_LENGTH_BITS = Util.Log2(Config.STRING_LENGTH_MAX);

        public void WriteString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var length = (uint)value.Length;
            Debug.Assert(length <= Config.STRING_LENGTH_MAX, value);
            if (value.Length > Config.STRING_LENGTH_MAX)
            {
                length = Config.STRING_LENGTH_MAX;
            }

            Write(STRING_LENGTH_BITS, length);
            for (int i = 0; i < length; i++)
            {
                Write(ASCII_BITS, ToASCII(value[i]));
            }
        }

        private byte ToASCII(char character)
        {
            byte value = 0;

            try
            {
                value = Convert.ToByte(character);
            }
            catch (OverflowException)
            {
                Console.WriteLine($"Cannot convert to simple ASCII: {character}");
                return 0;
            }

            if (value > 127)
            {
                Console.WriteLine($"Cannot convert to simple ASCII: {character}");
                return 0;
            }

            return value;
        }

        public string ReadString()
        {
            var builder = new StringBuilder("");
            var length = Read(STRING_LENGTH_BITS);
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)Read(ASCII_BITS));
            }

            return builder.ToString();
        }
    }
}
