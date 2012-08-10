using System.Collections.Generic;
using System.IO;
using System.Linq;
using bsharptree.example.simpledb.objectid;

namespace bsharptree.example.simpledb
{
    public class ObjectIdStore : IDataStore<ObjectId, byte[]>
    {
        private const int ObjectIdSize = 12;
        private readonly DataStore _dataStore;

        public ObjectIdStore(string directory)
            : this(new DataStore(directory, ObjectIdSize))
        {
        }

        public ObjectIdStore(string indexFile, string blobFile)
            : this(new DataStore(indexFile, blobFile, ObjectIdSize))
        {
        }

        public ObjectIdStore(Stream indexStream, Stream blobStream)
            : this(new DataStore(indexStream, blobStream, ObjectIdSize))
        {
        }

        public ObjectIdStore(DataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public void Dispose()
        {
            _dataStore.Dispose();
        }

        public byte[] Get(ObjectId key)
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

        public IEnumerable<ObjectId> EnumerateKeys()
        {
            return _dataStore.EnumerateKeys().Select(key => new ObjectId(key));
        }

        public void Save(ObjectId key, byte[] value)
        {
            _dataStore.Save(key.ToByteArray(), value);
        }

        public void Delete(ObjectId key)
        {
            _dataStore.Delete(key.ToByteArray());
        }

        public void Compact()
        {
            _dataStore.Compact();
        }
    }
}