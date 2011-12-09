using System;
using System.Collections.Generic;
using System.IO;
using bsharptree.exception;

namespace bsharptree.toolkit
{
    public static class ByteTools
    {
        public const int IntStorage = 4;
        public const int LongStorage = 8;
        public const int ShortStorage = 2;
        
        // there are probably libraries for this, but whatever...
        public static void Store(int theInt, byte[] toArray, int atIndex)
        {
            const int limit = IntStorage;

            if (atIndex + limit > toArray.Length)
                throw new BufferFileException("can't access beyond end of array");

            for (int i = 0; i < limit; i++)
            {
                var thebyte = (byte)(theInt & 0xff);
                toArray[atIndex + i] = thebyte;
                theInt = theInt >> 8;
            }
        }

        public static void Store(short theShort, byte[] toArray, int atIndex)
        {
            const int limit = ShortStorage;
            int theInt = theShort;

            if (atIndex + limit > toArray.Length)
                throw new BufferFileException("can't access beyond end of array");

            for (int i = 0; i < limit; i++)
            {
                var thebyte = (byte)(theInt & 0xff);
                toArray[atIndex + i] = thebyte;
                theInt = theInt >> 8;
            }
        }

        public static int Retrieve(byte[] toArray, int atIndex)
        {
            const int limit = IntStorage;

            if (atIndex + limit > toArray.Length)
                throw new BufferFileException("can't access beyond end of array");

            var result = 0;
            for (int i = 0; i < limit; i++)
            {
                byte thebyte = toArray[atIndex + limit - i - 1];
                result = result << 8;
                result = result | thebyte;
            }

            return result;
        }

        public static unsafe void Store(long theLong, byte[] toArray, int atIndex)
        {
            fixed (byte* numRef = toArray)
            {
                *((long*)(numRef + atIndex)) = theLong;
            }

            //const int limit = LongStorage;

            //if (atIndex + limit > toArray.Length)
            //    throw new BufferFileException("can't access beyond end of array");

            //for (int i = 0; i < limit; i++)
            //{
            //    var thebyte = (byte)(theLong & 0xff);
            //    toArray[atIndex + i] = thebyte;
            //    theLong = theLong >> 8;
            //}
        }

        public static long RetrieveLong(byte[] toArray, int atIndex)
        {
            return BitConverter.ToInt64(toArray, atIndex);
            //const int limit = LongStorage;
            //if (atIndex + limit > toArray.Length)
            //    throw new BufferFileException("can't access beyond end of array");

            //long result = 0;
            //for (int i = 0; i < limit; i++)
            //{
            //    byte thebyte = toArray[atIndex + limit - i - 1];
            //    result = result << 8;
            //    result = result | thebyte;
            //}

            //return result;
        }

        public static short RetrieveShort(byte[] toArray, int atIndex)
        {
            const int limit = ShortStorage;
            if (atIndex + limit > toArray.Length)
                throw new BufferFileException("can't access beyond end of array");

            int result = 0;
            for (int i = 0; i < limit; i++)
            {
                byte thebyte = toArray[atIndex + limit - i - 1];
                result = (result << 8);
                result = result | thebyte;
            }
            return (short)result;
        }

        public static void Upsert<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            // add or update value in dictionary
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return;
            }
            
            dictionary[key] = value;
        }

        public static unsafe void PutLong(long value, byte[] buffer, int offset)
        {
            fixed (byte* numRef = buffer)
            {
                *((long*)(numRef + offset)) = value;
            }
        }

        public static void CopyTo(this Stream input, Stream output)
        {
            var size = (input.CanSeek) ? Math.Min((int)(input.Length - input.Position), 0x2000) : 0x2000;
            var buffer = new byte[size];
            int n;
            do
            {
                n = input.Read(buffer, 0, buffer.Length);
                output.Write(buffer, 0, n);
            } while (n != 0);
        }

        public static void CopyTo(this MemoryStream input, Stream output)
        {
            output.Write(input.GetBuffer(), (int)input.Position, (int)(input.Length - input.Position));
        }

        public static void CopyTo(this Stream input, MemoryStream output)
        {
            if (input.CanSeek)
            {
                var pos = (int)output.Position;
                var length = (int)(input.Length - input.Position) + pos;
                output.SetLength(length);

                while (pos < length)
                    pos += input.Read(output.GetBuffer(), pos, length - pos);
            }
            else
                input.CopyTo(output);
        }
    }
}