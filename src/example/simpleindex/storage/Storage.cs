namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using bsharptree.definition;

    public class Storage<TKey, TRecordType, TStorageItem> : IDisposable
        where TKey : class, IEquatable<TKey>, IComparable<TKey>
        where TStorageItem : IStorageItem<TKey, TRecordType>
    {
        private readonly bool _shouldDisposeStreams;

        private readonly Stream _indexStream;
        private readonly Stream _recordStream;

        private readonly object _indexStreamLock = new object();
        private readonly object _recordStreamLock = new object();

        private readonly string _indexFilename;
        private readonly string _recordFilename;

        private readonly Func<Stream, RecordStorage<TRecordType>> _recordStorageFactory;
        private readonly IConverter<TKey, byte[]> _converter;
        private readonly int _maxKeySize;
        private readonly int _nodeSize;

        protected Storage(string directory, string defaultIndexFilename, string defaultRecordFilename, IConverter<TKey, byte[]> converter, Func<Stream, RecordStorage<TRecordType>> recordStorageFactory, int maxKeySize, int nodeSize)
            : this(
                StreamLoadingTools.DefaultFile(directory, defaultIndexFilename),
                StreamLoadingTools.DefaultFile(directory, defaultRecordFilename),
                converter, recordStorageFactory, maxKeySize, nodeSize)
        {
        }

        protected Storage(string indexFilename, string recordFilename, IConverter<TKey, byte[]> converter, Func<Stream, RecordStorage<TRecordType>> recordStorageFactory, int maxKeySize, int nodeSize)
            : this(
                File.Open(indexFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read),
                File.Open(recordFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), 
                converter, recordStorageFactory, maxKeySize, nodeSize, 
                true)
        {
            _indexFilename = indexFilename;
            _recordFilename = recordFilename;
        }

        protected Storage(Stream indexStream, Stream recordStream, IConverter<TKey, byte[]> converter, Func<Stream, RecordStorage<TRecordType>> recordStorageFactory, int maxKeySize, int nodeSize, bool shouldDisposeStreams = false)
        {
            _indexStream = indexStream;
            _recordStream = recordStream;
            _recordStorageFactory = recordStorageFactory;
            _converter = converter;
            _maxKeySize = maxKeySize;
            _nodeSize = nodeSize;

            _shouldDisposeStreams = shouldDisposeStreams;

            SetupStorages();
        }

        public ITreeIndex<TKey, long> Index { get; private set; }

        public RecordStorage<TRecordType> RecordStorage { get; private set; }

        public virtual void Add(TStorageItem item)
        {
            Add(item, true);
        }
        protected virtual void Add(TStorageItem item, bool withCommit)
        {
            Index[((IStorageItem<TKey, TRecordType>)item).Key] = RecordStorage.Add(((IStorageItem<TKey, TRecordType>)item).Value);
            
            if(withCommit)
                Index.Commit();
        }

        public virtual TStorageItem Get(TKey key)
        {
            return GetStorageItem(key, RecordStorage.Get(Index[key]));
        }

        public IEnumerable<TKey> EnumerateKeys()
        {
            lock (_indexStreamLock)
            {
                var key = Index.FirstKey();

                if (key != null)
                    yield return key;

                while (true)
                {
                    if (key != null)
                        key = Index.NextKey(key);

                    if (key == null)
                        break;

                    yield return key;
                }
            }
        }

        public virtual void MergeWith(Storage<TKey, TRecordType, TStorageItem> storage)
        {
            lock (_indexStreamLock)
            {
                lock (_recordStreamLock)
                {
                    var tempIndexFilename = _indexFilename + ".tmp";
                    var tempRecordFilename = _recordFilename + ".tmp";

                    using (var newIndexStream = string.IsNullOrEmpty(_indexFilename) ? (Stream)new MemoryStream() : File.Open(tempIndexFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read))
                    using (var newBlobStream = string.IsNullOrEmpty(_recordFilename) ? (Stream)new MemoryStream() : File.Open(tempRecordFilename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read))
                    using (var newStorage = new Storage<TKey, TRecordType, TStorageItem>(newIndexStream, newBlobStream, _converter, _recordStorageFactory, _maxKeySize, _nodeSize, true))
                    {
                        // merge and add keys
                        foreach (var key in EnumerateKeys())
                        {
                            var offset = Index[key];
                            var record = RecordStorage.Get(offset);

                            if(storage.Index.ContainsKey(key))
                            {
                                var thierOffset = storage.Index[key];
                                var thierRecord = storage.RecordStorage.Get(thierOffset);
                                record = MergeRecords(record, thierRecord);
                            }

                            newStorage.Add(GetStorageItem(key, record), false);
                        }

                        // add remainder
                        foreach (var key in storage.EnumerateKeys())
                        {
                            if (Index.ContainsKey(key)) continue;
                            newStorage.Add(storage.Get(key), false);
                        }

                        newStorage.Index.Commit();

                        // copy tmp streams to current and reset
                        CopyStream(newIndexStream, _indexStream);
                        CopyStream(newBlobStream, _recordStream);
                    }

                    if (File.Exists(tempIndexFilename)) File.Delete(tempIndexFilename);
                    if (File.Exists(tempRecordFilename)) File.Delete(tempRecordFilename);

                    SetupStorages();
                }
            }
        }

        private void SetupStorages()
        {
            Index = _indexStream.Length > 0
                ? BplusTreeLong<TKey>.SetupFromExistingStream(_indexStream, _converter)
                : BplusTreeLong<TKey>.InitializeInStream(_indexStream, _maxKeySize, _nodeSize, _converter);

            RecordStorage = _recordStorageFactory(_recordStream);
        }

        private static void CopyStream(Stream input, Stream output)
        {
            output.SetLength(input.Length);
            output.Seek(0, SeekOrigin.Begin);
            input.Seek(0, SeekOrigin.Begin);
            input.CopyTo(output);
            output.Seek(0, SeekOrigin.Begin);
        }

        protected virtual TRecordType MergeRecords(TRecordType record, TRecordType thierRecord)
        {
            throw new NotImplementedException();
        }

        protected virtual TStorageItem GetStorageItem(TKey key, TRecordType record)
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            if (!_shouldDisposeStreams) return;

            _indexStream.Dispose();
            _recordStream.Dispose();
        }
    }

    public struct StorageItem<TKey, TValue> : IStorageItem<TKey, TValue>
    {
        public StorageItem(TKey key, TValue value)
            : this()
        {
            Key = key;
            Value = value;
        }
        public TKey Key { get; private set; }

        public TValue Value { get; private set; }
    }
}