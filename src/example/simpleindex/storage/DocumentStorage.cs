namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.IO;
    using System.Text;

    using bsharptree.definition;

    public class DocumentStorage : Storage<toolkit.Guid, Stream, Document>
    {
        private const string DefaultDocumentIndexFilename = "document.ndx";

        private const string DefaultDocumentRecordFilename = "document.dat";
        
        private const int MaxKeySize = toolkit.Guid.Size;

        private const int NodeSize = 6;

        public DocumentStorage(string directory)
            : base(directory, DefaultDocumentIndexFilename, DefaultDocumentRecordFilename, toolkit.Guid.DefaultConverter, GetStorage, MaxKeySize, NodeSize)
        {
        }

        public DocumentStorage(string termIndexFilename, string termRecordFilename)
            : base(termIndexFilename, termRecordFilename, toolkit.Guid.DefaultConverter, GetStorage, MaxKeySize, NodeSize)
        {
        }

        public DocumentStorage(Stream termIndexStream, Stream termRecordStream, bool shouldDisposeStreams = false)
            : base(termIndexStream, termRecordStream, toolkit.Guid.DefaultConverter, GetStorage, MaxKeySize, NodeSize, shouldDisposeStreams)
        {
        }

        //public DocumentStorage(Stream documentIndexStream, Stream documentRecordStream)
        //    : this (
        //        documentIndexStream.Length > 0 
        //            ? BplusTreeLong<toolkit.Guid>.SetupFromExistingStream(documentIndexStream, toolkit.Guid.DefaultConverter)
        //            : BplusTreeLong<toolkit.Guid>.InitializeInStream(documentIndexStream, toolkit.Guid.Size, 6, toolkit.Guid.DefaultConverter),
        //        new DocumentRecordStorage(documentRecordStream))
        //{
        //}
        
        //public Document GetDocument(Guid documentGuid)
        //{
        //    var offset = Index[new toolkit.Guid(documentGuid)];
        //    return new Document(documentGuid, RecordStorage.Get(offset));
        //}

        //public void AddDocument(Document document)
        //{
        //    if (document.Key == Guid.Empty)
        //        document.Key = Guid.NewGuid();

        //    Index[new toolkit.Guid(document.Key)] = RecordStorage.Add(document.Value);
        //}

        public string GetContext(DocumentLocation documentLocation, int contextWidth = 25, Encoding encoding = null)
        {
            if (null == encoding)
                encoding = Encoding.UTF8;

            var bytesPadding = encoding.GetMaxByteCount(contextWidth);
            var document = Get(documentLocation.Document);
            var bodyStream = document.Value;

            var contextSpan = new Span {
                Start = documentLocation.Span.Start > bytesPadding ? documentLocation.Span.Start - bytesPadding : 0,
                End = documentLocation.Span.End + bytesPadding < bodyStream.Length
                        ? documentLocation.Span.End + bytesPadding
                        : bodyStream.Length
            };


            bodyStream.Seek(contextSpan.Start, SeekOrigin.Begin);

            var buffer = bodyStream.ReadBytes((int)contextSpan.Length);

            return encoding.GetString(buffer);
        }
    
        protected override Document GetStorageItem(toolkit.Guid key, Stream record)
        {
 	        return new Document(key, record);
        }
        
        private static RecordStorage<Stream> GetStorage(Stream stream)
        {
            return new DocumentRecordStorage(stream);
        }

        protected override Stream MergeRecords(Stream record, Stream thierRecord)
        {
            throw new NotSupportedException("Document streams should never need to be merged.");
        }
    }
}