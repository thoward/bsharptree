namespace bsharptree.example.simpleindex
{
    using System.IO;

    public class Utf8CharScanner
    {
        /// <summary>Default byte buffer size (2048).</summary>
        public const int DefaultBufferSize = 2048;

        /** Input stream. */

        /** Byte buffer. */
        private readonly byte[] _buffer;
        private readonly Stream _stream;

        /** Offset into buffer. */
        private int _bufferOffset;

        /** Surrogate character. */
        private CharLocation _surrogate;

        public Utf8CharScanner(Stream inputStream)
            : this(inputStream, DefaultBufferSize)
        {
        }

        public Utf8CharScanner(Stream inputStream, int size)
            : this(inputStream, new byte[size])
        {
        }

        public Utf8CharScanner(Stream inputStream, byte[] buffer)
        {
            _stream = inputStream;
            _buffer = buffer;
        }

        /// <summary>
        /// Read a single character.  This method will block until a character is
        /// available, an I/O error occurs, or the end of the stream is reached.
        /// 
        /// <p> Subclasses that intend to support efficient single-character input
        /// should override this method.</p>
        /// </summary>
        /// <throws>IOException  If an I/O error occurs</throws>
        /// <returns>
        /// The character read, as an integer in the range 0 to 16383
        /// (<tt>0x00-0xffff</tt>), or -1 if the end of the stream has
        /// been reached
        /// </returns>
        public CharLocation Read()
        {
            // decode character
            var c = _surrogate;

            if (_surrogate.Value != null)
            {
                _surrogate.Value = null;
                return c;
            }

            // NOTE: We use the index into the buffer if there are remaining
            //       bytes from the last block read. -Ac
            var index = 0;

            // get first byte
            var byteZero = index == _bufferOffset
                               ? _stream.ReadByte()
                               : _buffer[index++] & 0x00FF;

            if (byteZero == -1)
            {
                // -1 == EOF
                return CharLocation.Empty;
            }

            if (byteZero < 0x80)
            {
                // UTF-8:   [0xxx xxxx]
                // Unicode: [0000 0000] [0xxx xxxx]
                // Single Byte Char
                c = new CharLocation { Value = (char)byteZero, ByteSpan = new Span { Start = GetCurrentPosition(index), End = GetCurrentPosition(index) + 1 } };
            }
            else if ((byteZero & 0xE0) == 0xC0 && (byteZero & 0x1E) != 0)
            {
                // UTF-8:   [110y yyyy] [10xx xxxx]
                // Unicode: [0000 0yyy] [yyxx xxxx]
                var start = GetCurrentPosition(index);
                c = new CharLocation
                    {
                        Value = ReadTwoByteChar(index, byteZero),
                        ByteSpan = { Start = start, End = GetCurrentPosition(index) + 1 }
                    };
            }
            else if ((byteZero & 0xF0) == 0xE0)
            {
                // UTF-8:   [1110 zzzz] [10yy yyyy] [10xx xxxx]
                // Unicode: [zzzz yyyy] [yyxx xxxx]
                var start = GetCurrentPosition(index);
                c = new CharLocation
                    {
                        Value = ReadThreeByteChar(index, byteZero),
                        ByteSpan = { Start = start, End = GetCurrentPosition(index) + 1 }
                    };
            }
            else if ((byteZero & 0xF8) == 0xF0)
            {
                // UTF-8:   [1111 0uuu] [10uu zzzz] [10yy yyyy] [10xx xxxx]*
                // Unicode: [1101 10ww] [wwzz zzyy] (high surrogate)
                //          [1101 11yy] [yyxx xxxx] (low surrogate)
                //          * uuuuu = wwww + 1
                var start = GetCurrentPosition(index);
                c = new CharLocation
                    {
                        Value = ReadFourByteChar(index, byteZero),
                        ByteSpan = { Start = start, End = GetCurrentPosition(index) + 1 }
                    };
            }
            else
            {
                ThrowInvalidByteException(1, 1, byteZero);
            }

            return c;
        }

        private long GetCurrentPosition(int index)
        {
            return index == _bufferOffset ? _stream.Position - 1 : (_stream.Position - 1) - index;
        }

        private char? ReadFourByteChar(int index, int byteZero)
        {
            var byte1 = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byte1 == -1)
                ThrowExpectedByteException(2, 4);

            if ((byte1 & 0xC0) != 0x80 || ((byte1 & 0x30) == 0 && (byteZero & 0x07) == 0))
                ThrowInvalidByteException(2, 3, byte1);

            var byte2 = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byte2 == -1)
                ThrowExpectedByteException(3, 4);

            if ((byte2 & 0xC0) != 0x80)
                ThrowInvalidByteException(3, 3, byte2);

            var byte3 = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byte3 == -1)
                ThrowExpectedByteException(4, 4);

            if ((byte3 & 0xC0) != 0x80)
                ThrowInvalidByteException(4, 4, byte3);

            var uuuuu = ((byteZero << 2) & 0x001C) | ((byte1 >> 4) & 0x0003);

            if (uuuuu > 0x10)
                ThrowInvalidSurrogateException(uuuuu);

            var wwww = uuuuu - 1;

            var highSurrogate = 0xD800 | ((wwww << 6) & 0x03C0) | ((byte1 << 2) & 0x003C) | ((byte2 >> 4) & 0x0003);
            var lowSurrogate = 0xDC00 | ((byte2 << 6) & 0x03C0) | (byte3 & 0x003F);

            _surrogate.Value = (char?)lowSurrogate;
            return (char?)highSurrogate;
        }

        private char? ReadThreeByteChar(int index, int byteZero)
        {
            var byte1 = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byte1 == -1)
                ThrowExpectedByteException(2, 3);

            if ((byte1 & 0xC0) != 0x80 || (byteZero == 0xED && byte1 >= 0xA0) || ((byteZero & 0x0F) == 0 && (byte1 & 0x20) == 0))
                ThrowInvalidByteException(2, 3, byte1);

            var byteTwo = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byteTwo == -1)
                ThrowExpectedByteException(3, 3);

            if ((byteTwo & 0xC0) != 0x80)
                ThrowInvalidByteException(3, 3, byteTwo);

            return (char?)(((byteZero << 12) & 0xF000) | ((byte1 << 6) & 0x0FC0) | (byteTwo & 0x003F));
        }

        private char? ReadTwoByteChar(int index, int byteZero)
        {
            var byte1 = index == _bufferOffset ? _stream.ReadByte() : _buffer[index++] & 0x00FF;

            if (byte1 == -1)
                ThrowExpectedByteException(2, 2);

            if ((byte1 & 0xC0) != 0x80)
                ThrowInvalidByteException(2, 2, byte1);

            return (char?)(((byteZero << 6) & 0x07C0) | (byte1 & 0x003F));
        }

        /// <summary>
        /// Read characters into a portion of an array.  This method will block
        /// until some input is available, an I/O error occurs, or the end of the
        /// stream is reached.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset at which to start storing characters</param>
        /// <param name="length">Maximum number of characters to read</param>
        /// <throws>IOException  If an I/O error occurs</throws>
        /// <returns>
        /// The number of characters read, or -1 if the end of the
        /// stream has been reached
        /// </returns>
        public int Read(char[] buffer, int offset, int length)
        {
            // read bytes
            var outputbufferIndex = offset;
            var count = 0;

            if (_bufferOffset == 0)
            {
                // adjust length to read
                if (length > _buffer.Length)
                {
                    length = _buffer.Length;
                }

                // handle surrogate
                if (_surrogate.Value.HasValue)
                {
                    buffer[outputbufferIndex++] = _surrogate.Value.Value;
                    _surrogate = CharLocation.Empty;
                    length--;
                }

                // perform read operation
                count = _stream.Read(_buffer, 0, length);
                if (count == -1)
                {
                    return -1;
                }
                count += outputbufferIndex - offset;
            }
            else
            {
                // skip read; last character was in error
                // NOTE: Having an offset value other than zero means that there was
                //       an error in the last character read. In this case, we have
                //       skipped the read so we don't consume any bytes past the
                //       error. By signalling the error on the next block read we
                //       allow the method to return the most valid characters that
                //       it can on the previous block read. -Ac
                count = _bufferOffset;
                _bufferOffset = 0;
            }

            // convert bytes to characters
            var total = count;
            int inputBufferIndex;
            byte currentByte;
            const byte emptyByte = 0;
            for (inputBufferIndex = 0; inputBufferIndex < total; inputBufferIndex++)
            {
                currentByte = _buffer[inputBufferIndex];
                if (currentByte >= emptyByte)
                {
                    buffer[outputbufferIndex++] = (char)currentByte;
                }
                else
                {
                    break;
                }
            }

            for (; inputBufferIndex < total; inputBufferIndex++)
            {
                currentByte = _buffer[inputBufferIndex];

                // UTF-8:   [0xxx xxxx]
                // Unicode: [0000 0000] [0xxx xxxx]
                if (currentByte >= emptyByte)
                {
                    buffer[outputbufferIndex++] = (char)currentByte;
                    continue;
                }

                // UTF-8:   [110y yyyy] [10xx xxxx]
                // Unicode: [0000 0yyy] [yyxx xxxx]
                var byteZero = currentByte & 0x0FF;
                if ((byteZero & 0xE0) == 0xC0 && (byteZero & 0x1E) != 0)
                {
                    int byte1;
                    if (++inputBufferIndex < total)
                    {
                        byte1 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte1 = _stream.ReadByte();
                        if (byte1 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _bufferOffset = 1;
                                return outputbufferIndex - offset;
                            }
                            ThrowExpectedByteException(2, 2);
                        }
                        count++;
                    }

                    if ((byte1 & 0xC0) != 0x80)
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _bufferOffset = 2;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(2, 2, byte1);
                    }

                    var c = ((byteZero << 6) & 0x07C0) | (byte1 & 0x003F);

                    buffer[outputbufferIndex++] = (char)c;

                    count -= 1;

                    continue;
                }

                // UTF-8:   [1110 zzzz] [10yy yyyy] [10xx xxxx]
                // Unicode: [zzzz yyyy] [yyxx xxxx]
                if ((byteZero & 0xF0) == 0xE0)
                {
                    int byte1;
                    if (++inputBufferIndex < total)
                    {
                        byte1 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte1 = _stream.ReadByte();

                        if (byte1 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _bufferOffset = 1;

                                return outputbufferIndex - offset;
                            }

                            ThrowExpectedByteException(2, 3);
                        }

                        count++;
                    }

                    if ((byte1 & 0xC0) != 0x80 || (byteZero == 0xED && byte1 >= 0xA0) || ((byteZero & 0x0F) == 0 && (byte1 & 0x20) == 0))
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _bufferOffset = 2;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(2, 3, byte1);
                    }

                    int byte2;
                    if (++inputBufferIndex < total)
                    {
                        byte2 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte2 = _stream.ReadByte();

                        if (byte2 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _buffer[1] = (byte)byte1;
                                _bufferOffset = 2;

                                return outputbufferIndex - offset;
                            }

                            ThrowExpectedByteException(3, 3);
                        }

                        count++;
                    }

                    if ((byte2 & 0xC0) != 0x80)
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _buffer[2] = (byte)byte2;
                            _bufferOffset = 3;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(3, 3, byte2);
                    }

                    var c = ((byteZero << 12) & 0xF000) | ((byte1 << 6) & 0x0FC0) | (byte2 & 0x003F);
                    buffer[outputbufferIndex++] = (char)c;
                    count -= 2;
                    continue;
                }

                // UTF-8:   [1111 0uuu] [10uu zzzz] [10yy yyyy] [10xx xxxx]*
                // Unicode: [1101 10ww] [wwzz zzyy] (high surrogate)
                //          [1101 11yy] [yyxx xxxx] (low surrogate)
                //          * uuuuu = wwww + 1
                if ((byteZero & 0xF8) == 0xF0)
                {
                    int byte1;
                    if (++inputBufferIndex < total)
                    {
                        byte1 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte1 = _stream.ReadByte();

                        if (byte1 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _bufferOffset = 1;
                                return outputbufferIndex - offset;
                            }

                            ThrowExpectedByteException(2, 4);
                        }

                        count++;
                    }

                    if ((byte1 & 0xC0) != 0x80 || ((byte1 & 0x30) == 0 && (byteZero & 0x07) == 0))
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _bufferOffset = 2;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(2, 4, byte1);
                    }

                    int byte2;

                    if (++inputBufferIndex < total)
                    {
                        byte2 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte2 = _stream.ReadByte();

                        if (byte2 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _buffer[1] = (byte)byte1;
                                _bufferOffset = 2;

                                return outputbufferIndex - offset;
                            }

                            ThrowExpectedByteException(3, 4);
                        }

                        count++;
                    }

                    if ((byte2 & 0xC0) != 0x80)
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _buffer[2] = (byte)byte2;
                            _bufferOffset = 3;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(3, 4, byte2);
                    }

                    int byte3;

                    if (++inputBufferIndex < total)
                    {
                        byte3 = _buffer[inputBufferIndex] & 0x00FF;
                    }
                    else
                    {
                        byte3 = _stream.ReadByte();

                        if (byte3 == -1)
                        {
                            if (outputbufferIndex > offset)
                            {
                                _buffer[0] = (byte)byteZero;
                                _buffer[1] = (byte)byte1;
                                _buffer[2] = (byte)byte2;
                                _bufferOffset = 3;

                                return outputbufferIndex - offset;
                            }

                            ThrowExpectedByteException(4, 4);
                        }

                        count++;
                    }

                    if ((byte3 & 0xC0) != 0x80)
                    {
                        if (outputbufferIndex > offset)
                        {
                            _buffer[0] = (byte)byteZero;
                            _buffer[1] = (byte)byte1;
                            _buffer[2] = (byte)byte2;
                            _buffer[3] = (byte)byte3;
                            _bufferOffset = 4;

                            return outputbufferIndex - offset;
                        }

                        ThrowInvalidByteException(4, 4, byte2);
                    }

                    // decode bytes into surrogate characters
                    var uuuuu = ((byteZero << 2) & 0x001C) | ((byte1 >> 4) & 0x0003);

                    if (uuuuu > 0x10)
                        ThrowInvalidSurrogateException(uuuuu);

                    var wwww = uuuuu - 1;
                    var zzzz = byte1 & 0x000F;
                    var yyyyyy = byte2 & 0x003F;
                    var xxxxxx = byte3 & 0x003F;

                    var highSurrogate = 0xD800 | ((wwww << 6) & 0x03C0) | (zzzz << 2) | (yyyyyy >> 4);
                    var lowSurrogate = 0xDC00 | ((yyyyyy << 6) & 0x03C0) | xxxxxx;

                    // set characters
                    buffer[outputbufferIndex++] = (char)highSurrogate;

                    if ((count -= 2) <= length)
                    {
                        buffer[outputbufferIndex++] = (char)lowSurrogate;
                    }
                    else
                    {
                        // reached the end of the char buffer; save low surrogate for the next read
                        _surrogate.Value = (char)lowSurrogate;
                        --count;
                    }

                    continue;
                }

                // error
                if (outputbufferIndex > offset)
                {
                    _buffer[0] = (byte)byteZero;
                    _bufferOffset = 1;

                    return outputbufferIndex - offset;
                }

                ThrowInvalidByteException(1, 1, byteZero);
            }

            return count;
        }

        /// <summary>
        /// Skip characters.  This method will block until some characters are
        /// available, an I/O error occurs, or the end of the stream is reached.
        /// </summary>
        /// <param name="charsToSkip">The number of characters to skip</param>
        /// <throws>IOException  If an I/O error occurs</throws>
        /// <returns>The number of characters actually skipped</returns>
        public long Skip(long charsToSkip)
        {
            var remaining = charsToSkip;
            var ch = new char[_buffer.Length];
            do
            {
                var length = ch.Length < remaining ? ch.Length : (int)remaining;
                var count = Read(ch, 0, length);

                if (count <= 0)
                    break;

                remaining -= count;

            } while (remaining > 0);

            return charsToSkip - remaining;
        }

        public void Reset()
        {
            _bufferOffset = 0;
            _surrogate = CharLocation.Empty;
        }

        public void Dispose()
        {
            if (null != _stream)
                _stream.Dispose();
        }

        /// <summary>
        /// Throws an exception for expected byte. 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="count"></param>
        private static void ThrowExpectedByteException(int position, int count)
        {
            throw new MalformedByteSequenceException("ExpectedByte. position: " + position + " count: " + count);
        }

        /// <summary>
        /// Throws an exception for invalid byte. 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="c"></param>
        private static void ThrowInvalidByteException(int position, int count, int c)
        {
            throw new MalformedByteSequenceException("InvalidBytes. position: " + position + " count: " + count);
        }

        /// <summary>
        /// Throws an exception for invalid surrogate bits. 
        /// </summary>
        /// <param name="uuuuu"></param>
        private static void ThrowInvalidSurrogateException(int uuuuu)
        {
            throw new MalformedByteSequenceException("InvalidHighSurrogate" + uuuuu.ToString("X2"));
        }
    }
}