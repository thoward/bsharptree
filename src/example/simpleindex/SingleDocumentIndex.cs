using bsharptree.example.simpleindex.analysis;

namespace bsharptree.example.simpleindex
{
    using System;
    using System.IO;
    using System.Linq;

    using bsharptree.example.simpleindex.storage;

    public class SingleDocumentIndex
    {
        public SingleDocumentIndex(Document document, IAnalyzer analyzer)
            : this(document, analyzer, new MemoryStream(), new MemoryStream(), new MemoryStream(), new MemoryStream())
        {
        }
        public SingleDocumentIndex(Document document, IAnalyzer analyzer, Stream termIndexStream, Stream termRecordStorageStream, Stream documentIndexStream, Stream documentRecordStorageStream)
            : this(document, analyzer, new TermStorage(termIndexStream, termRecordStorageStream), new DocumentStorage(documentIndexStream, documentRecordStorageStream) )
        {
        }
        public SingleDocumentIndex(Document document, IAnalyzer analyzer, TermStorage termStorage, DocumentStorage documentStorage)
        {
            TermStorage = termStorage;
            DocumentStorage = documentStorage;
            AddDocument(document, analyzer);
        }

        public TermStorage TermStorage { get; set; }

        public DocumentStorage DocumentStorage { get; set; }
                
        private void AddDocument(Document document, IAnalyzer analyzer)
        {
            if (document.Key == Guid.Empty)
                document.Key = Guid.NewGuid();

            DocumentStorage.Add(document);

            System.Diagnostics.Debug.Assert(document.Value.CanSeek);
            document.Value.Seek(0, SeekOrigin.Begin);
            var termPositions = analyzer.GetTermPositions(document.Value);
            foreach(var termPositionsGroup in termPositions.GroupBy(a=>a.Term))
            {
                var term = new Term(
                    termPositionsGroup.Key,
                    termPositionsGroup.Select(a => new DocumentLocation { Document = document.Key, Span = a.Span }));

                TermStorage.Add(term);
            }
        }

        public void MergeWith(SingleDocumentIndex subIndex)
        {
            TermStorage.MergeWith(subIndex.TermStorage);
            DocumentStorage.MergeWith(subIndex.DocumentStorage);
        }
    }
}