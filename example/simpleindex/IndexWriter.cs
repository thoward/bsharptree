using bsharptree.example.simpleindex.analysis;

namespace bsharptree.example.simpleindex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using bsharptree.example.simpleindex.storage;

    public class IndexWriter : IDisposable
    {
        public IndexWriter()
        {
            MergeFactor = 10;
            FlushFrequency = 10;
            SubIndexes = new List<SingleDocumentIndex>();
        }
        
        public IndexWriter(string directory)
            : this(new TermStorage(directory), new DocumentStorage(directory))
        {
        }

        public IndexWriter(string termIndexFilename, string termRecordFilename, string documentIndexFilename, string documentRecordFilename)
            : this(new TermStorage(termIndexFilename, termRecordFilename), new DocumentStorage(documentIndexFilename, documentRecordFilename))
        {
        }

        public IndexWriter(Stream termIndexStream, Stream termRecordStream, Stream documentIndexStream, Stream documentRecordStream, bool shouldDisposeStreams = false)
            : this(new TermStorage(termIndexStream, termRecordStream), new DocumentStorage(documentIndexStream, documentRecordStream))
        {
        }

        public IndexWriter(TermStorage termStorage, DocumentStorage documentStorage)
            : this()
        {
            TermStorage = termStorage;
            DocumentStorage = documentStorage;
        }

        /// <summary>
        ///  How many documents to index before merging partial indexes
        /// </summary>
        public int MergeFactor { get; set; }

        /// <summary>
        /// How many merge operations to perform before flushing to primary output streams
        /// </summary>
        public int FlushFrequency { get; set; }

        public List<SingleDocumentIndex> SubIndexes { get; set; }

        public TermStorage TermStorage { get; set; }
        public DocumentStorage DocumentStorage { get; set; }

        private readonly object _subIndexesLock = new object();

        private int _flushCounter;

        public void AddDocument(Document document, IAnalyzer analyzer)
        {
            var singleDocumentIndex = new SingleDocumentIndex(document, analyzer);
            lock (_subIndexesLock)
            {
                SubIndexes.Add(singleDocumentIndex);

                if (SubIndexes.Count >= MergeFactor) 
                    MergeSubIndexes();

                if (_flushCounter >= FlushFrequency) 
                    FlushIndexes();
            }
        }

        private void FlushIndexes()
        {
            // SubIndexes should only hold a single record at this point..
            Debug.Assert(SubIndexes.Count == 1);

            TermStorage.MergeWith(SubIndexes[0].TermStorage);
            DocumentStorage.MergeWith(SubIndexes[0].DocumentStorage);
        }

        private void MergeSubIndexes()
        {
            var firstSubIndex = SubIndexes[0];
            for (int i = 1; i < SubIndexes.Count; i++)
            {
                firstSubIndex.MergeWith(SubIndexes[i]);
            }
            SubIndexes.Clear();
            SubIndexes.Add(firstSubIndex);
            _flushCounter++;
        }

        public void Dispose()
        {
            lock (_subIndexesLock)
            {
                if (SubIndexes.Count > 1) MergeSubIndexes();

                FlushIndexes();
            }

            if(default(TermStorage) != TermStorage)
                TermStorage.Dispose();

            if (default(DocumentStorage) != DocumentStorage)
                DocumentStorage.Dispose();
        }
    }
}