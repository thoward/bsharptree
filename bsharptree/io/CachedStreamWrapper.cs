using System.IO;

namespace bsharptree.io
{
    /// <summary>
    /// A simple wrapper for a <see cref="Stream"/> which caches the position and length values, 
    /// instead of calculating them every time. This improves performance against large files.
    /// </summary>
    public class CachedStreamWrapper : Stream
    {
        public CachedStreamWrapper(Stream stream)
        {
            _stream = stream;
            _length = stream.Length;
        }

        private readonly Stream _stream;
        private long _length;
        private long _position;

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = _stream.Seek(offset, origin);
            return _position;
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
            _length = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _stream.Read(buffer, offset, count);
            _position += bytesRead;
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            if (_position + count > _length)
                _length = _position + count;
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get
            {
                return _length;
                //_stream.Length; 
            }
        }

        public override long Position
        {
            get
            {
                return _position;
                //return _stream.Position;
            }
            set
            {
                _position = value;
                _stream.Position = value;
            }
        }
    }
}