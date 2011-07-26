using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using bsharptree.definition;
using bsharptree.exception;
using bsharptree.io;
using bsharptree.toolkit;

namespace bsharptree
{
    /// <summary>
    /// BPlus tree implementation mapping strings to bytes with fixed key length
    /// </summary>
    public class BplusTreeBytes<TKey> : ITreeIndex<TKey, byte[]> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        private const int Defaultblocksize = 1024;
        private const int Defaultnodesize = 32;

        private readonly LinkedFile _archive;
        private readonly HashSet<long> _freeChunksOnAbort = new HashSet<long>();
        private readonly HashSet<long> _freeChunksOnCommit = new HashSet<long>();
        private readonly BplusTreeLong<TKey> _tree;

        public BplusTreeBytes(BplusTreeLong<TKey> tree, LinkedFile archive)
        {
            _tree = tree;
            _archive = archive;
        }

        #region IHtmlPrintable Members

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(_tree.ToString());
            sb.Append("free on commit " + _freeChunksOnCommit.Count + " ::");

            foreach (var chunkNumber in _freeChunksOnCommit)
                sb.Append(" " + chunkNumber);

            sb.AppendLine();

            sb.Append("free on abort " + _freeChunksOnAbort.Count + " ::");

            foreach (var chunkNumber in _freeChunksOnAbort)
                sb.Append(" " + chunkNumber);

            return sb.ToString(); // archive info not included
        }

        #endregion

        #region ITreeIndex<string,byte[]> Members

        public void Shutdown()
        {
            _tree.Shutdown();
            _archive.Shutdown();
        }

        public IConverter<byte[], byte[]> ValueConverter { get; set; }

        public void Recover(bool correctErrors)
        {
            _tree.Recover(correctErrors);

            var chunksInUse = new Dictionary<long, TKey>();
            var key = _tree.FirstKey();

            while (key == default(TKey))
            {
                var buffernumber = _tree[key];
                
                if (chunksInUse.ContainsKey(buffernumber))
                    throw new BplusTreeException("buffer number " + buffernumber + " associated with more than one key '" + key + "' and '" + chunksInUse[buffernumber] + "'");

                chunksInUse.Upsert(buffernumber, key);
                key = _tree.NextKey(key);
            }

            // also consider the un-deallocated chunks to be in use
            foreach (var buffernumber in _freeChunksOnCommit)
                chunksInUse[buffernumber] = default(TKey);
                    //"awaiting commit";

            _archive.Recover(chunksInUse, correctErrors);
        }

        public void RemoveKey(TKey key)
        {
            var map = _tree[key];

            if (_freeChunksOnAbort.Contains(map))
            {
                // free it now
                _freeChunksOnAbort.Remove(map);
                _archive.ReleaseBuffers(map);
            }
            else
            {
                // free when committed
                _freeChunksOnCommit.Add(map);
            }

            _tree.RemoveKey(key);
        }

        public TKey FirstKey()
        {
            return _tree.FirstKey();
        }

        public TKey NextKey(TKey afterThisKey)
        {
            return _tree.NextKey(afterThisKey);
        }

        public bool ContainsKey(TKey key, out byte[] value)
        {
            long map;

            if (_tree.ContainsKey(key, out map))
            {
                value = _archive.GetChunk(map);
                return true;
            }
            value = null;
            return false;
        }
        public bool ContainsKey(TKey key)
        {
            return _tree.ContainsKey(key);
        }

        public byte[] this[TKey key]
        {
            get
            {
                long map;

                if (!_tree.ContainsKey(key, out map))
                    throw new BplusTreeKeyMissingException("Key not found.");

                return _archive.GetChunk(map);
            }
            set
            {
                var storage = _archive.StoreNewChunk(value, 0, value.Length);
                _freeChunksOnAbort.Add(storage);

                long valueFound;
                if (_tree.ContainsKey(key, out valueFound))
                {
                    //this.archive.ReleaseBuffers(valueFound);
                    if (_freeChunksOnAbort.Contains(valueFound))
                    {
                        // free it now
                        _freeChunksOnAbort.Remove(valueFound);
                        _archive.ReleaseBuffers(valueFound);
                    }
                    else
                    {
                        // release at commit.
                        _freeChunksOnCommit.Add(valueFound);
                    }
                }

                _tree[key] = storage;
            }
        }

        public IConverter<TKey, byte[]> KeyConverter { get; set; }

        public void Commit()
        {
            // store all new bufferrs
            _archive.Flush();
            
            // commit the tree
            _tree.Commit();

            // at this point the new buffers have been committed, now free the old ones
            //this.FreeChunksOnCommit.Sort();
            var toFree = _freeChunksOnCommit.ToList();

            toFree.Sort();
            toFree.Reverse();

            foreach (var chunknumber in toFree)
                _archive.ReleaseBuffers(chunknumber);

            _archive.Flush();

            ClearBookKeeping();
        }

        public void Abort()
        {
            var toFree = _freeChunksOnAbort.ToList();

            toFree.Sort();
            toFree.Reverse();

            foreach (var chunknumber in toFree)
                _archive.ReleaseBuffers(chunknumber);

            _tree.Abort();
            _archive.Flush();

            ClearBookKeeping();
        }

        public void SetFootPrintLimit(int limit)
        {
            _tree.SetFootPrintLimit(limit);
        }

        #endregion

        public static BplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int keyLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var treefile = new CachedStreamWrapper(new FileStream(treefileName, FileMode.CreateNew, FileAccess.ReadWrite));
            var blockfile = new CachedStreamWrapper(new FileStream(blockfileName, FileMode.CreateNew, FileAccess.ReadWrite));

            return Initialize(treefile, blockfile, keyLength, cultureId, nodesize, buffersize, keyConverter);
        }

        public static BplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int keyLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var treefile = new CachedStreamWrapper(new FileStream(treefileName, FileMode.CreateNew, FileAccess.ReadWrite));
            var blockfile = new CachedStreamWrapper(new FileStream(blockfileName, FileMode.CreateNew, FileAccess.ReadWrite));

            return Initialize(treefile, blockfile, keyLength, cultureId, keyConverter);
        }

        public static BplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var treefile = new CachedStreamWrapper(new FileStream(treefileName, FileMode.CreateNew, FileAccess.ReadWrite));
            var blockfile = new CachedStreamWrapper(new FileStream(blockfileName, FileMode.CreateNew, FileAccess.ReadWrite));

            return Initialize(treefile, blockfile, keyLength, keyConverter);
        }

        public static BplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int keyLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeLong<TKey>.InitializeInStream(treefile, keyLength, nodesize, cultureId, keyConverter);
            var archive = LinkedFile.InitializeLinkedFileInStream(blockfile, buffersize);

            return new BplusTreeBytes<TKey>(tree, archive);
        }

        public static BplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int keyLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return Initialize(treefile, blockfile, keyLength, cultureId, Defaultnodesize, Defaultblocksize, keyConverter);
        }

        public static BplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var cultureId = CultureInfo.InvariantCulture.LCID;

            return Initialize(treefile, blockfile, keyLength, cultureId, Defaultnodesize, Defaultblocksize, keyConverter);
        }

        public static BplusTreeBytes<TKey> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeLong<TKey>.SetupFromExistingStream(treefile, keyConverter);
            var archive = LinkedFile.SetupFromExistingStream(blockfile);

            return new BplusTreeBytes<TKey>(tree, archive);
        }

        public static BplusTreeBytes<TKey> ReOpen(string treefileName, string blockfileName, FileAccess access, IConverter<TKey, byte[]> keyConverter)
        {
            var treefile = new CachedStreamWrapper(new FileStream(treefileName, FileMode.Open, access));
            var blockfile = new CachedStreamWrapper(new FileStream(blockfileName, FileMode.Open, access));

            return ReOpen(treefile, blockfile, keyConverter);
        }

        public static BplusTreeBytes<TKey> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            return ReOpen(treefileName, blockfileName, FileAccess.ReadWrite, keyConverter);
        }

        public static BplusTreeBytes<TKey> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            return ReOpen(treefileName, blockfileName, FileAccess.Read, keyConverter);
        }

        /// <summary>
        /// Use non-culture sensitive total order on binary strings.
        /// </summary>
        public void NoCulture()
        {
            _tree.DontUseCulture = true;
            _tree.CultureContext = null;
        }

        public int MaxKeyLength()
        {
            return _tree.MaxKeyLength();
        }

        private void ClearBookKeeping()
        {
            _freeChunksOnCommit.Clear();
            _freeChunksOnAbort.Clear();
        }
    }
}