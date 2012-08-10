namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.IO;

    public class StreamSegment : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _endOffset;
        private readonly long _startOffset;

        public StreamSegment(Stream baseStream, long startOffset, long endOffset)
        {
            _baseStream = baseStream;
            _startOffset = startOffset;
            _endOffset = endOffset;
        }

        public override bool CanRead { get { return _baseStream.CanRead; } }

        public override bool CanSeek { get { return _baseStream.CanSeek; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return _endOffset - _startOffset; } }

        public override long Position
        {
            get
            {
                if (_baseStream.Position < _startOffset) return 0;
                if (_baseStream.Position > _endOffset) return Length;

                return _baseStream.Position - _startOffset;
            }
            set { Seek(value, SeekOrigin.Begin); }
        }


        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if(((origin == SeekOrigin.Begin || origin == SeekOrigin.End) && offset > Length - 1) || (origin == SeekOrigin.Current && Position + offset > Length))
            {
                throw new ArgumentException("Invalid offset", "offset");
            }

            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _baseStream.Seek(_startOffset + offset, SeekOrigin.Begin);
                case SeekOrigin.Current:
                    return _baseStream.Seek(offset, SeekOrigin.Current);
                case SeekOrigin.End:
                    return _baseStream.Seek(_baseStream.Length - _endOffset + offset, SeekOrigin.End);
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }   
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}