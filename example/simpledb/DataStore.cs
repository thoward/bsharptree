using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bsharptree.toolkit;

namespace bsharptree.example.simpledb
{
    public class DataStore : IDataStore<byte[], byte[]>
    {
        private const int DefaultMaxKeySize = 24;
        private const string DefaultIndexFilename = "keys.ndx";
        private const string DefaultBlobFilename = "values.dat";

        private readonly object _blobHandle = new object();
        private readonly Stream _blobStream;
        private BplusTreeLong<ByteCollection> _index;
        private readonly object _indexHandle = new object();
        private readonly Stream _indexStream;
        private readonly int _maxKeySize;
        private readonly bool _shouldDisposeStreams;
        private readonly string _indexFilename;
        private readonly string _blobFilename;

        public DataStore(string directory, int maxKeySize = DefaultMaxKeySize)
            : this(DefaultFile(directory, DefaultIndexFilename), DefaultFile(directory, DefaultBlobFilename), maxKeySize)
        {
        }

        public DataStore(string indexFilename, string blobFilename, int maxKeySize = DefaultMaxKeySize)
            : this(
                File.Open(indexFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read),
                File.Open(blobFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), maxKeySize)
        {
            _shouldDisposeStreams = true;
            _indexFilename = indexFilename;
            _blobFilename = blobFilename;
        }

        public DataStore(Stream indexStream, Stream blobStream, int maxKeySize = DefaultMaxKeySize)
        {
            _indexStream = indexStream;
            _blobStream = blobStream;
            _maxKeySize = maxKeySize;

            SetupIndex();
        }

        private void SetupIndex()
        {
            _index =     _indexStream.Length > 0
                             ? BplusTreeLong<ByteCollection>.SetupFromExistingStream(_indexStream, ByteCollection.DefaultConverter)
                             : BplusTreeLong<ByteCollection>.InitializeInStream(_indexStream, _maxKeySize, 6, ByteCollection.DefaultConverter);
        }

        public byte[] Get(byte[] key)
        {
            return Get(LookupOffset(key));
        }

        public byte[] Get(long recordHandle)
        {
            Record record;
            lock (_blobHandle)
            {
                _blobStream.Seek(recordHandle, SeekOrigin.Begin);

                record = Record.FromStream(_blobStream);
            }

            if (record.Status == RecordStatus.Deleted)
                throw new Exception("Record deleted.");

            return record.Value;
        }

        public void Save(byte[] key, byte[] value)
        {
            var actualKey = new ByteCollection(key);
            long offset;

            bool existing;
            lock (_indexHandle)
            {
                existing = _index.ContainsKey(actualKey, out offset);
            }

            if (existing)
            {
                // do update

                // fetch value, if new value is shorter, write in same location, 
                // but zero out extra content tail
                // if longer, mark previous record as deleted, and write a new record, 
                // updating the index with new offset info

                int length;
                var lengthBuffer = new byte[4];
                lock (_blobHandle)
                {
                    _blobStream.Seek(offset + 1, SeekOrigin.Begin);
                    _blobStream.Read(lengthBuffer, 0, 4);
                    length = BitConverter.ToInt32(lengthBuffer, 0);
                }

                if (value.Length <= length)
                {
                    var zeroBuffer = new byte[length - value.Length];

                    lock (_blobHandle)
                    {
                        _blobStream.Seek(offset + 1, SeekOrigin.Begin);
                        _blobStream.Write(BitConverter.GetBytes(value.Length), 0, 4);
                        _blobStream.Write(value, 0, value.Length);
                        if (zeroBuffer.Length > 0)
                            _blobStream.Write(zeroBuffer, 0, zeroBuffer.Length);
                        _blobStream.Flush();
                    }
                }
                else
                {
                    MarkBlobEntryDeleted(offset);
                    offset = AddNewBlobEntry(value);
                    lock (_indexHandle)
                    {
                        _index.UpdateKey(actualKey, offset);
                        _index.Commit();
                    }
                }
            }
            else
            {
                // do insert
                offset = AddNewBlobEntry(value);

                lock (_indexHandle)
                {
                    _index[actualKey] = offset;
                    _index.Commit();
                }
            }
        }

        private long AddNewBlobEntry(byte[] value)
        {
            long offset;
            var record = new Record {Status = RecordStatus.Record, Value = value};
            byte[] recordBytes = record.ToBytes();

            lock (_blobHandle)
            {
                _blobStream.Seek(0, SeekOrigin.End);
                offset = _blobStream.Position;
                _blobStream.Write(recordBytes, 0, recordBytes.Length);
                _blobStream.Flush();
            }
            return offset;
        }

        public void Delete(byte[] key)
        {
            long offset = LookupOffset(key);

            // soft delete...
            lock (_indexHandle)
            {
                _index.RemoveKey(new ByteCollection(key));
                _index.Commit();
            }

            lock (_blobHandle)
            {
                MarkBlobEntryDeleted(offset);
            }
        }

        private void MarkBlobEntryDeleted(long offset)
        {
            lock (_blobHandle)
            {
                _blobStream.Seek(offset, SeekOrigin.Begin);
                _blobStream.WriteByte((byte) RecordStatus.Deleted);
                _blobStream.Flush();
            }
        }

        public void Compact()
        {
            lock (_indexHandle)
            {
                lock (_blobHandle)
                {
                    var tempIndexFilename = _indexFilename + ".tmp";
                    var tempBlobFilename = _blobFilename + ".tmp";

                    using (var newIndexStream = string.IsNullOrEmpty(_indexFilename) ? (Stream) new MemoryStream() : File.Open(tempIndexFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read))
                    using (var newBlobStream = string.IsNullOrEmpty(_blobFilename) ? (Stream)new MemoryStream() : File.Open(tempBlobFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read))
                    using (var newDataStore = new DataStore(newIndexStream, newBlobStream, _maxKeySize))
                    {
                        foreach (var key in EnumerateKeysInternal().ToList())
                        {
                            var offset = _index[key];
                            var data = Get(offset);

                            newDataStore.Save(key.Data, data);
                        }

                        // copy tmp streams to current and reset

                        CopyStream(newIndexStream, _indexStream);
                        CopyStream(newBlobStream, _blobStream);
                    }

                    if(File.Exists(tempIndexFilename)) File.Delete(tempIndexFilename);
                    if(File.Exists(tempBlobFilename)) File.Delete(tempBlobFilename);
                    SetupIndex();
                }
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            output.SetLength(input.Length);
            output.Seek(0, SeekOrigin.Begin);
            input.Seek(0, SeekOrigin.Begin);
            input.CopyTo(output);
            output.Seek(0, SeekOrigin.Begin);
        }

        public CompactStatistics GetCompactStatistics()
        {
            var statistics = new CompactStatistics();

            lock (_indexHandle)
            {
                lock (_blobHandle)
                {
                    foreach (var offset in EnumerateKeysInternal().ToList().Select(key => _index[key]))
                    {
                        statistics.RecordCount++;
                        var lengthBuffer = new byte[4];

                        _blobStream.Seek(offset + 1, SeekOrigin.Begin);
                        _blobStream.Read(lengthBuffer, 0, 4);


                        var recordSize = 5 + BitConverter.ToInt32(lengthBuffer, 0);
                        statistics.PostCompactBlobSize += (ulong) recordSize;
                    }
                }
            }
             
            var nodecount = 
                GuessInternalNodeCount(statistics.RecordCount, (ulong)_index.NodeSize);
            var nodeBytes = GuessNodeBytes(_index.KeyLength, _index.NodeSize);
            statistics.EstimatedPostCompactIndexSize =  (nodecount * nodeBytes) + (ulong) _index.Headersize;
            statistics.IndexSize = (ulong)_indexStream.Length;
            statistics.BlobSize = (ulong)_blobStream.Length;
            
            return statistics;
        }

        private static ulong GuessInternalNodeCount(ulong recordCount, ulong indexNodeSize)
        {
            if(recordCount > indexNodeSize)
            {
                var nodecount = (recordCount - 1) / indexNodeSize + 1;
                return nodecount + GuessInternalNodeCount(nodecount, indexNodeSize);
            }
            return 1;
        }

        private static ulong GuessNodeBytes(int keylength, int nodeSize)
        {
            return (ulong)( 
                1 // node type (byte) 
                + 8 // seek position (long)
                + (( 2 // keylength (short)
                     + keylength // key
                     + 8 ) * nodeSize) ); // payload (long)
        }

        public IEnumerable<byte[]> EnumerateKeys()
        {
            return EnumerateKeysInternal().ToList().Select(key => key.Data);
        }

        public void Dispose()
        {
            if (!_shouldDisposeStreams)
                return;

            if (default(Stream) != _indexStream)
                _indexStream.Dispose();

            if (default(Stream) != _blobStream)
                _blobStream.Dispose();
        }

        private long LookupOffset(byte[] key)
        {
            if (key.Length > _maxKeySize)
                throw new Exception("Invalid key size.");

            var actualKey = new ByteCollection(key);
            lock (_indexHandle)
            {
                return _index[actualKey];
            }
        }

        private IEnumerable<ByteCollection> EnumerateKeysInternal()
        {
            lock (_indexHandle)
            {
                var key = _index.FirstKey();

                if (key != null)
                    yield return key;

                while (true)
                {
                    if (key != null)
                        key = _index.NextKey(key);

                    if (key == null)
                        break;

                    yield return key;
                }
            }
        }

        private static string DefaultFile(string directory, string filename)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, filename);
        }
    }
}