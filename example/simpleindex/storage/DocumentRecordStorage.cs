namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.IO;

    public class DocumentRecordStorage : RecordStorage<Stream>
    {
        public DocumentRecordStorage(Stream stream)
            : base(stream)
        {
            _stream = stream;
        }

        public override Stream Get(long offset)
        {
            lock(_streamLock)
            {
                _stream.Seek(offset, SeekOrigin.Begin);
                var length = _stream.ReadInt64();
                return new StreamSegment(_stream, offset + 8, offset + 8 + length);
            }
        }

        public override long Add(Stream body)
        {
            lock (_streamLock)
            {
                var offset = _stream.Length;
                _stream.Seek(0, SeekOrigin.End);
                _stream.Write(BitConverter.GetBytes(body.Length), 0, 8);
                
                if(body.CanSeek)
                    body.Seek(0, SeekOrigin.Begin);

                body.CopyTo(_stream);
                return offset;
            }
        }
    }
}