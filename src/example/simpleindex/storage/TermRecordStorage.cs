namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class TermRecordStorage : RecordStorage<IEnumerable<DocumentLocation>>
    {
        public TermRecordStorage(Stream stream)
            : base(stream)
        {
        }

        public override long Add(IEnumerable<DocumentLocation> locations)
        {
            // doc-count | [ guid | pos-count | [position ... ] ...]
            long documentCountOffset;
            long currentPosition;
            
            lock(_streamLock)
            {
                _stream.Seek(0, SeekOrigin.End);
                currentPosition = documentCountOffset = _stream.Position;
                
                // write empty length placeholder
                _stream.Write(new byte[8], 0, 8);
                currentPosition += 8;
            }
            
            long documentCount = 0;

            foreach(var locationsByDoc in locations.GroupBy(a=> a.Document))
            {
                long locationCountOffset;
                lock(_streamLock)
                {
                    // in case our position changed between locks, we do a seeks to our known position
                    // this should enable better multi-threaded performance
                    _stream.Seek(currentPosition, SeekOrigin.Begin);
                    _stream.Write(locationsByDoc.Key.ToByteArray(), 0, toolkit.Guid.Size);
                    currentPosition += toolkit.Guid.Size;

                    locationCountOffset = currentPosition;

                    // another empty count placeholder
                    _stream.Write(new byte[8], 0, 8);
                    currentPosition += 8;
                }

                long locationCount = 0;
                foreach(var location in locationsByDoc)
                {
                    lock (_streamLock)
                    {
                        _stream.Seek(currentPosition, SeekOrigin.Begin);
                        _stream.Write(BitConverter.GetBytes(location.Span.Start), 0, 8);
                        _stream.Write(BitConverter.GetBytes(location.Span.End), 0, 8);
                        currentPosition += 16;
                    }

                    locationCount++;
                }

                lock(_streamLock)
                {
                    _stream.Seek(locationCountOffset, SeekOrigin.Begin);
                    _stream.Write(BitConverter.GetBytes(locationCount), 0, 8);
                }

                documentCount++;
            }

            lock (_streamLock)
            {
                _stream.Seek(documentCountOffset, SeekOrigin.Begin);
                _stream.Write(BitConverter.GetBytes(documentCount), 0, 8);
            }

            return documentCountOffset;
        }

        public override IEnumerable<DocumentLocation> Get(long offset)
        {
            var currentPosition = offset;
            long documentCount;
            lock(_streamLock)
            {
                _stream.Seek(offset, SeekOrigin.Begin);
                documentCount = _stream.ReadInt64();
                currentPosition += 8;
            }
            for (long i = 0; i < documentCount; i++)
            {
                Guid documentGuid;
                long locationCount;
                //read guid
                lock (_streamLock)
                {
                    _stream.Seek(currentPosition, SeekOrigin.Begin);

                    documentGuid = _stream.ReadGuid();
                    locationCount = _stream.ReadInt64();
                    currentPosition += toolkit.Guid.Size + 8;
                }
                for (int j = 0; j < locationCount; j++)
                {
                    Span span;
                    lock (_streamLock)
                    {
                        _stream.Seek(currentPosition, SeekOrigin.Begin);
                        span = _stream.ReadSpan();
                        currentPosition += 16;
                    }   
                    yield return new DocumentLocation { Document = documentGuid, Span = span };
                }
            }
        }
    }
}