using System.IO;
using bsharptree.exception;
using bsharptree.toolkit;

namespace bsharptree.io
{
    /// <summary>
    /// Provides an indexed object which maps to buffers in an underlying file object
    /// </summary>
    public class BufferFile
    {
        public const byte Version = 0;

        public static byte[] HeaderPrefix = {98, 112, 78, 98, 102};
        public static int MinBufferSize = 16;

        private readonly Stream _fromFile;
        private readonly int _headerSize;
        private readonly long _seekStart;

        public BufferFile(Stream fromFile, int buffersize, long seekStart)
        {
            _seekStart = seekStart;
            _fromFile = fromFile;
            _headerSize = HeaderPrefix.Length + ByteTools.IntStorage + 1; // +version byte+4 bytes for buffersize
            
            Buffersize = buffersize;
            
            SanityCheck();
        }

        public BufferFile(Stream fromFile, int buffersize) :
            this(fromFile, buffersize, 0)
        {
            // just start seek at 0
        }

        public int Buffersize { get; private set; }

        public static BufferFile SetupFromExistingStream(Stream fromfile)
        {
            return SetupFromExistingStream(fromfile, 0);
        }

        public static BufferFile SetupFromExistingStream(Stream fromfile, long startSeek)
        {
            var result = new BufferFile(fromfile, 100, startSeek); // dummy buffer size for now
            result.ReadHeader();
            return result;
        }

        public static BufferFile InitializeBufferFileInStream(Stream fromfile, int buffersize)
        {
            return InitializeBufferFileInStream(fromfile, buffersize, 0);
        }

        public static BufferFile InitializeBufferFileInStream(Stream fromfile, int buffersize, long startSeek)
        {
            var result = new BufferFile(fromfile, buffersize, startSeek);
            result.SetHeader();
            return result;
        }

        private void SanityCheck()
        {
            if (Buffersize < MinBufferSize)
                throw new BufferFileException("buffer size too small " + Buffersize);

            if (_seekStart < 0)
                throw new BufferFileException("can't start at negative position " + _seekStart);

        }

        public void GetBuffer(long buffernumber, byte[] toArray, int startingAt, int length)
        {
            if (buffernumber >= NextBufferNumber())
                throw new BufferFileException("last buffer is " + NextBufferNumber() + " not " + buffernumber);

            if (length > Buffersize)
                throw new BufferFileException("buffer size too small for retrieval " + Buffersize + " need " + length);

            var seekPosition = BufferSeek(buffernumber);

            _fromFile.Seek(seekPosition, SeekOrigin.Begin);
            _fromFile.Read(toArray, startingAt, length);
        }

        public void SetBuffer(long buffernumber, byte[] fromArray, int startingAt, int length)
        {
            //System.Diagnostics.Debug.WriteLine("<br> setting buffer "+buffernumber);
            if (length > Buffersize)
                throw new BufferFileException("buffer size too small for assignment " + Buffersize + " need " + length);

            if (buffernumber > NextBufferNumber())
                throw new BufferFileException("cannot skip buffer numbers from " + NextBufferNumber() + " to " + buffernumber);

            var seekPosition = BufferSeek(buffernumber);

            // need to fill with junk if beyond eof?
            _fromFile.Seek(seekPosition, SeekOrigin.Begin);
            
            //this.fromFile.Seek(seekPosition);
            _fromFile.Write(fromArray, startingAt, length);
        }

        private void SetHeader()
        {
            var header = MakeHeader();
            _fromFile.Seek(_seekStart, SeekOrigin.Begin);
            _fromFile.Write(header, 0, header.Length);
        }

        public void Flush()
        {
            _fromFile.Flush();
        }

        private void ReadHeader()
        {
            var header = new byte[_headerSize];
            _fromFile.Seek(_seekStart, SeekOrigin.Begin);
            _fromFile.Read(header, 0, _headerSize);

            var index = 0;

            // check prefix
            foreach (var b in HeaderPrefix)
            {
                if (header[index] != b)
                    throw new BufferFileException("invalid header prefix");

                index++;
            }

            // skip version (for now)
            index++;
            
            // read buffersize
            Buffersize = ByteTools.Retrieve(header, index);
            SanityCheck();
            
            //this.header = header;
        }

        public byte[] MakeHeader()
        {
            var result = new byte[_headerSize];
            
            HeaderPrefix.CopyTo(result, 0);
            result[HeaderPrefix.Length] = Version;
            ByteTools.Store(Buffersize, result, HeaderPrefix.Length + 1);

            return result;
        }

        private long BufferSeek(long bufferNumber)
        {
            if (bufferNumber < 0)
                throw new BufferFileException("buffer number cannot be negative");

            return _seekStart + _headerSize + (Buffersize * bufferNumber);
        }

        public long NextBufferNumber()
        {
            // round up the buffer number based on the current file length
            long filelength = _fromFile.Length;
            long bufferspace = filelength - _headerSize - _seekStart;
            long nbuffers = bufferspace/Buffersize;
            long remainder = bufferspace%Buffersize;

            return remainder > 0 
                ? nbuffers + 1 
                : nbuffers;
        }
    }

    
}