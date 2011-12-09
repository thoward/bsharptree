using bsharptree.example.simpleindex.analysis;

namespace bsharptree.example.simpleindex.storage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using bsharptree.definition;
    using bsharptree.toolkit;

    using System;

    public class TermStorage : Storage<string, IEnumerable<DocumentLocation>, Term>
    {
        private const string DefaultTermIndexFilename = "term.ndx";

        private const string DefaultTermRecordFilename = "term.dat";

        private const int DefaultMaxKeySize = 256;

        private const int DefaultNodeSize = 6;
        
        public TermStorage(string directory)
            : base(directory, DefaultTermIndexFilename, DefaultTermRecordFilename, StringConverter.Default, GetStorage, DefaultMaxKeySize, DefaultNodeSize)
        {
        }

        public TermStorage(string termIndexFilename, string termRecordFilename)
            : base(termIndexFilename, termRecordFilename, StringConverter.Default, GetStorage, DefaultMaxKeySize, DefaultNodeSize)
        {
        }

        public TermStorage(Stream termIndexStream, Stream termRecordStream, bool shouldDisposeStreams = false)
            : base(termIndexStream, termRecordStream, StringConverter.Default, GetStorage, DefaultMaxKeySize, DefaultNodeSize, shouldDisposeStreams)
        {
        }

        protected override IEnumerable<DocumentLocation> MergeRecords(IEnumerable<DocumentLocation> record, IEnumerable<DocumentLocation> thierRecord)
        {
            return new List<IEnumerable<DocumentLocation>> { record, thierRecord }.SelectMany(a => a).Distinct();
        }

        protected override Term GetStorageItem(string key, IEnumerable<DocumentLocation> record)
        {
            return new Term(key, record);
        }

        private static RecordStorage<IEnumerable<DocumentLocation>> GetStorage(Stream stream)
        {
            return new TermRecordStorage(stream);
        }
    }
}