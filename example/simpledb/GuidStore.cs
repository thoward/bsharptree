using System.Collections.Generic;
using System.IO;
using System.Linq;
using bsharptree.toolkit;

namespace bsharptree.example.simpledb
{
    public class GuidStore : IDataStore<Guid, byte[]>
    {
        private const int GuidSize = 16;
        private readonly DataStore _dataStore;

        public GuidStore(string directory)
            : this(new DataStore(directory, GuidSize))
        {
        }

        public GuidStore(string indexFile, string blobFile)
            : this(new DataStore(indexFile, blobFile, GuidSize))
        {
        }

        public GuidStore(Stream indexStream, Stream blobStream)
            : this(new DataStore(indexStream, blobStream, GuidSize))
        {
        }

        public GuidStore(DataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public void Dispose()
        {
            _dataStore.Dispose();
        }

        public byte[] Get(Guid key)
        {
            return _dataStore.Get(key.ToByteArray());
        }

        public byte[] Get(long recordHandle)
        {
            return _dataStore.Get(recordHandle);
        }

        public CompactStatistics GetCompactStatistics()
        {
            return _dataStore.GetCompactStatistics();
        }

        public IEnumerable<Guid> EnumerateKeys()
        {
            return _dataStore.EnumerateKeys().Select(key => new Guid(key));
        }

        public void Save(Guid key, byte[] value)
        {
            _dataStore.Save(key.ToByteArray(), value);
        }

        public void Delete(Guid key)
        {
            _dataStore.Delete(key.ToByteArray());
        }

        public void Compact()
        {
            _dataStore.Compact();
        }
    }
}