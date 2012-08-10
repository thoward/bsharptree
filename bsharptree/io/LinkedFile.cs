using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bsharptree.exception;
using bsharptree.toolkit;

namespace bsharptree.io
{
    /// <summary>
    /// Chunked singly linked file with garbage collection.
    /// </summary>
    public class LinkedFile
    {
        private const long NullBufferPointer = -1;
        private const byte Free = 0;
        private const byte Head = 1;
        private const byte Body = 2;
        public static byte[] HeaderPrefix = {98, 112, 78, 108, 102};
        public static byte Version;
        public static int Minbuffersize = 20;

        // next pointer and indicator flag
        public static int BufferOverhead = ByteTools.LongStorage + 1;
        protected readonly int HeaderSize;
        protected BufferFile Buffers;
        protected Stream FromFile;

        private readonly long _seekStart;
        private int _bufferSize;
        private long _freeListHead = NullBufferPointer;
        
        private bool _headerDirty = true;
        private long _recentNewBufferNumber = NullBufferPointer;

        public LinkedFile(int bufferSize, long seekStart)
        {
            _seekStart = seekStart;
            
            //this.buffers = buffers;
            
            _bufferSize = bufferSize;
            
            // markers+version byte+buffersize+freelisthead
            HeaderSize = HeaderPrefix.Length + 1 + ByteTools.IntStorage + ByteTools.LongStorage;
            SanityCheck();
        }

        public static LinkedFile SetupFromExistingStream(Stream fromfile)
        {
            return SetupFromExistingStream(fromfile, 0);
        }

        public static LinkedFile SetupFromExistingStream(Stream fromfile, long startSeek)
        {
            var result = new LinkedFile(100, startSeek) // dummy buffer size for now
                             {
                                 FromFile = fromfile
                             }; 

            result.ReadHeader();
            result.Buffers = BufferFile.SetupFromExistingStream(fromfile, startSeek + result.HeaderSize);

            return result;
        }

        private void ReadHeader()
        {
            var header = new byte[HeaderSize];
            FromFile.Seek(_seekStart, SeekOrigin.Begin);
            FromFile.Read(header, 0, HeaderSize);

            var index = 0;
            
            // check prefix
            foreach (var b in HeaderPrefix)
            {
                if (header[index] != b)
                    throw new LinkedFileException("invalid header prefix");
                
                index++;
            }

            // skip version (for now)
            index++;
            
            // read buffersize
            _bufferSize = ByteTools.Retrieve(header, index);
            
            index += ByteTools.IntStorage;
            _freeListHead = ByteTools.RetrieveLong(header, index);
            
            SanityCheck();
            
            _headerDirty = false;
        }

        public static LinkedFile InitializeLinkedFileInStream(Stream fromfile, int buffersize)
        {
            return InitializeLinkedFileInStream(fromfile, buffersize, 0);
        }

        public static LinkedFile InitializeLinkedFileInStream(Stream fromfile, int buffersize, long startSeek)
        {
            var result = new LinkedFile(buffersize, startSeek)
                             {
                                 FromFile = fromfile
                             };

            result.SetHeader();
            
            // buffersize should be increased by overhead...
            result.Buffers = BufferFile.InitializeBufferFileInStream(fromfile, buffersize + BufferOverhead, startSeek + result.HeaderSize);
            return result;
        }

        public void SetHeader()
        {
            var header = MakeHeader();

            FromFile.Seek(_seekStart, SeekOrigin.Begin);
            FromFile.Write(header, 0, header.Length);

            _headerDirty = false;
        }

        public byte[] MakeHeader()
        {
            var result = new byte[HeaderSize];
            
            HeaderPrefix.CopyTo(result, 0);
            result[HeaderPrefix.Length] = Version;
            
            var index = HeaderPrefix.Length + 1;
            ByteTools.Store(_bufferSize, result, index);

            index += ByteTools.IntStorage;
            ByteTools.Store(_freeListHead, result, index);

            return result;
        }

        public void Recover<T>(Dictionary<long, T> chunksInUse, bool fixErrors)
        {
            // find missing space and recover it
            CheckStructure(chunksInUse, fixErrors);
        }

        private void SanityCheck()
        {
            if (_seekStart < 0)
                throw new LinkedFileException("cannot seek negative " + _seekStart);

            if (_bufferSize < Minbuffersize)
                throw new LinkedFileException("buffer size too small " + _bufferSize);
        }

        public void Shutdown()
        {
            FromFile.Flush();
            FromFile.Close();
        }

        private byte[] ParseBuffer(long bufferNumber, out byte type, out long nextBufferNumber)
        {
            var thebuffer = new byte[_bufferSize];
            var fullbuffer = new byte[_bufferSize + BufferOverhead];
            Buffers.GetBuffer(bufferNumber, fullbuffer, 0, fullbuffer.Length);
            type = fullbuffer[0];
            nextBufferNumber = ByteTools.RetrieveLong(fullbuffer, 1);
            Buffer.BlockCopy(fullbuffer, BufferOverhead, thebuffer, 0, _bufferSize);
            return thebuffer;
        }

        private void SetBuffer(long buffernumber, byte type, byte[] thebuffer, int start, int length, long nextBufferNumber)
        {
            //System.Diagnostics.Debug.WriteLine(" storing chunk type "+type+" at "+buffernumber);
            if (_bufferSize < length)
                throw new LinkedFileException("buffer size too small " + _bufferSize + "<" + length);

            var fullbuffer = new byte[length + BufferOverhead];
            fullbuffer[0] = type;
            ByteTools.Store(nextBufferNumber, fullbuffer, 1);
            
            if (thebuffer != null)
                Buffer.BlockCopy(thebuffer, start, fullbuffer, BufferOverhead, length);

            Buffers.SetBuffer(buffernumber, fullbuffer, 0, fullbuffer.Length);
        }

        private void DeallocateBuffer(long buffernumber)
        {
            //System.Diagnostics.Debug.WriteLine(" deallocating "+buffernumber);
            // should be followed by resetting the header eventually.
            SetBuffer(buffernumber, Free, null, 0, 0, _freeListHead);
            _freeListHead = buffernumber;
            _headerDirty = true;
        }

        private long AllocateBuffer()
        {
            if (_freeListHead != NullBufferPointer)
            {
                // reallocate a freed buffer
                long result = _freeListHead;
                byte buffertype;
                long nextFree;

                ParseBuffer(result, out buffertype, out nextFree);

                if (buffertype != Free)
                    throw new LinkedFileException("free head buffer not marked free");

                _freeListHead = nextFree;
                _headerDirty = true;
                _recentNewBufferNumber = NullBufferPointer;
                return result;
            }

            // allocate a new buffer
            var nextbuffernumber = Buffers.NextBufferNumber();

            // the previous buffer has been allocated but not yet written.  It must be written before the following one...
            if (_recentNewBufferNumber == nextbuffernumber)
                nextbuffernumber++;

            _recentNewBufferNumber = nextbuffernumber;
            
            return nextbuffernumber;
        }

        public void CheckStructure<T>()
        {
            CheckStructure<T>(null, false);
        }

        public void CheckStructure<T>(Dictionary<long, T> chunksInUse, bool fixErrors)
        {
            var buffernumberToType = new Dictionary<long, byte>();
            var buffernumberToNext = new Dictionary<long, long>();

            var visited = new Dictionary<long, long>();
            
            long lastBufferNumber = Buffers.NextBufferNumber();
            
            for (long buffernumber = 0; buffernumber < lastBufferNumber; buffernumber++)
            {
                byte buffertype;
                long nextBufferNumber;
                ParseBuffer(buffernumber, out buffertype, out nextBufferNumber);

                buffernumberToType.Upsert(buffernumber, buffertype);
                buffernumberToNext.Upsert(buffernumber, nextBufferNumber);
            }

            // traverse the freelist
            var thisFreeBuffer = _freeListHead;
            while (thisFreeBuffer != NullBufferPointer)
            {
                if (visited.ContainsKey(thisFreeBuffer))
                    throw new LinkedFileException("cycle in freelist " + thisFreeBuffer);

                visited.Upsert(thisFreeBuffer, thisFreeBuffer);

                var thetype = buffernumberToType[thisFreeBuffer];
                var nextbuffernumber = buffernumberToNext[thisFreeBuffer];

                if (thetype != Free)
                    throw new LinkedFileException("free list element not marked free " + thisFreeBuffer);

                thisFreeBuffer = nextbuffernumber;
            }

            // traverse all nodes marked head
            var allchunks = new HashSet<long>();
            for (long buffernumber = 0; buffernumber < lastBufferNumber; buffernumber++)
            {
                var thetype = buffernumberToType[buffernumber];
                if (thetype != Head) continue;

                if (visited.ContainsKey(buffernumber))
                    throw new LinkedFileException("head buffer already visited " + buffernumber);

                allchunks.Add(buffernumber);
                visited.Upsert(buffernumber, buffernumber);
                    
                var bodybuffernumber = buffernumberToNext[buffernumber];
                    
                while (bodybuffernumber != NullBufferPointer)
                {
                    var bodytype = buffernumberToType[bodybuffernumber];
                    var nextbuffernumber = buffernumberToNext[bodybuffernumber];

                    if (visited.ContainsKey(bodybuffernumber))
                        throw new LinkedFileException("body buffer visited twice " + bodybuffernumber);

                    visited[bodybuffernumber] = bodytype;

                    if (bodytype != Body)
                        throw new LinkedFileException("body buffer not marked body " + thetype);

                    bodybuffernumber = nextbuffernumber;
                }

                // check retrieval
                GetChunk(buffernumber);
            }

            // make sure all were visited
            for (long buffernumber = 0; buffernumber < lastBufferNumber; buffernumber++)
            {
                if (!visited.ContainsKey(buffernumber))
                    throw new LinkedFileException("buffer not found either as data or free " + buffernumber);
            }

            // check against in use list
            if (chunksInUse == null) return;

            var notInUse = new List<long>();
            foreach (var entry in chunksInUse.Where(entry => !allchunks.Contains(entry.Key)))
            {
                //System.Diagnostics.Debug.WriteLine("\r\n<br>allocated chunks "+allchunks.Count);
                //foreach (DictionaryEntry d1 in allchunks) 
                //{
                //	System.Diagnostics.Debug.WriteLine("\r\n<br>found "+d1.Key);
                //}
                throw new LinkedFileException("buffer in used list not found in linked file " + entry.Key + " " + entry.Value);
            }

            foreach (var buffernumber in allchunks.Where(buffernumber => !chunksInUse.ContainsKey(buffernumber)))
            {
                if (!fixErrors)
                    throw new LinkedFileException("buffer in linked file not in used list " + buffernumber);

                notInUse.Add(buffernumber);
            }

            notInUse.Sort();
            notInUse.Reverse();

            foreach (var buffernumber in notInUse)
                ReleaseBuffers(buffernumber);
        }

        public byte[] GetChunk(long headBufferNumber)
        {
            // get the head, interpret the length
            byte buffertype;
            long nextBufferNumber;
            byte[] buffer = ParseBuffer(headBufferNumber, out buffertype, out nextBufferNumber);

            int length = ByteTools.Retrieve(buffer, 0);
            
            if (length < 0)
                throw new LinkedFileException("negative length block? must be garbage: " + length);

            if (buffertype != Head)
                throw new LinkedFileException("first buffer not marked HEAD");

            var result = new byte[length];

            // read in the data from the first buffer
            int firstLength = _bufferSize - ByteTools.IntStorage;

            if (firstLength > length)
                firstLength = length;

            Buffer.BlockCopy(buffer, ByteTools.IntStorage, result, 0, firstLength);
            int stored = firstLength;

            while (stored < length)
            {
                // get the next buffer
                long thisBufferNumber = nextBufferNumber;
                buffer = ParseBuffer(thisBufferNumber, out buffertype, out nextBufferNumber);
                
                int nextLength = _bufferSize;
                
                if (length - stored < nextLength)
                    nextLength = length - stored;

                Buffer.BlockCopy(buffer, 0, result, stored, nextLength);
                
                stored += nextLength;
            }

            return result;
        }

        public long StoreNewChunk(byte[] fromArray, int startingAt, int length)
        {
            // get the first buffer as result value
            long currentBufferNumber = AllocateBuffer();
            //System.Diagnostics.Debug.WriteLine(" allocating chunk starting at "+currentBufferNumber);
            long result = currentBufferNumber;
            if (length < 0 || startingAt < 0)
                throw new LinkedFileException("cannot store negative length chunk (" + startingAt + "," + length + ")");

            int endingAt = startingAt + length;
            
            // special case: zero length chunk
            if (endingAt > fromArray.Length)
                throw new LinkedFileException("array doesn't have this much data: " + endingAt);

            // store header with length information
            var buffer = new byte[_bufferSize];
            ByteTools.Store(length, buffer, 0);
            int fromIndex = startingAt;
            int firstLength = _bufferSize - ByteTools.IntStorage;
            int stored = 0;
            
            if (firstLength > length)
                firstLength = length;

            Buffer.BlockCopy(fromArray, fromIndex, buffer, ByteTools.IntStorage, firstLength);
            stored += firstLength;
            fromIndex += firstLength;
            byte currentBufferType = Head;
            
            // store any remaining buffers (no length info)
            while (stored < length)
            {
                // store current buffer and get next block number
                long nextBufferNumber = AllocateBuffer();
                SetBuffer(currentBufferNumber, currentBufferType, buffer, 0, buffer.Length, nextBufferNumber);
                currentBufferNumber = nextBufferNumber;
                currentBufferType = Body;
                int nextLength = _bufferSize;
                if (stored + nextLength > length)
                    nextLength = length - stored;

                Buffer.BlockCopy(fromArray, fromIndex, buffer, 0, nextLength);
                stored += nextLength;
                fromIndex += nextLength;
            }

            // store final buffer
            SetBuffer(currentBufferNumber, currentBufferType, buffer, 0, buffer.Length, NullBufferPointer);
            return result;
        }

        public void Flush()
        {
            if (_headerDirty)
                SetHeader();

            Buffers.Flush();
        }

        public void ReleaseBuffers(long headBufferNumber)
        {
            // KISS
            //System.Diagnostics.Debug.WriteLine(" deallocating chunk starting at "+HeadBufferNumber);
            long nextbuffernumber;
            byte buffertype;
            
            ParseBuffer(headBufferNumber, out buffertype, out nextbuffernumber);
            
            if (buffertype != Head)
                throw new LinkedFileException("head buffer not marked HEAD");

            DeallocateBuffer(headBufferNumber);

            while (nextbuffernumber != NullBufferPointer)
            {
                long thisbuffernumber = nextbuffernumber;
                ParseBuffer(thisbuffernumber, out buffertype, out nextbuffernumber);

                if (buffertype != Body)
                    throw new LinkedFileException("body buffer not marked BODY");

                DeallocateBuffer(thisbuffernumber);
            }
        }
    }
}