using System;
using System.IO;
using bsharptree.definition;
using bsharptree.exception;
using bsharptree.io;

namespace bsharptree
{
    /// <summary>
    /// Bplustree with unlimited length strings (but only a fixed prefix is indexed in the tree directly).
    /// </summary>
    public class XBplusTreeBytes<TKey> : ITreeIndex<TKey, byte[]>
        where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        public int BucketSizeLimit = -1;
        public int PrefixLength;
        public BplusTreeBytes<TKey> Tree;

        public XBplusTreeBytes(BplusTreeBytes<TKey> tree, int prefixLength)
        {
            if (prefixLength < 3)
            {
                throw new BplusTreeException("prefix cannot be smaller than 3 :: " + prefixLength);
            }
            if (prefixLength > tree.MaxKeyLength())
            {
                throw new BplusTreeException("prefix length cannot exceed keylength for internal tree");
            }
            Tree = tree;
            PrefixLength = prefixLength;
        }

        #region ITreeIndex<TKey ,byte[]> Members

        public IConverter<byte[], byte[]> ValueConverter { get; set; }

        public void Recover(bool correctErrors)
        {
            Tree.Recover(correctErrors);
        }

        public void RemoveKey(TKey key)
        {
            XBucket<TKey, byte[]> bucket;
            TKey prefix;

            if (!TryFindBucketForPrefix(key, out bucket, out prefix, false))
            {
                throw new BplusTreeKeyMissingException("no such key to delete");
            }
            bucket.Remove(key);
            if (bucket.Count < 1)
            {
                Tree.RemoveKey(prefix);
            }
            else
            {
                Tree[prefix] = bucket.Dump();
            }
        }

        public TKey FirstKey()
        {
            XBucket<TKey, byte[]> bucket;
            var prefix = Tree.FirstKey();
            if (prefix == default(TKey))
                return default(TKey);

            TKey dummyprefix;
            
            bool found = TryFindBucketForPrefix(prefix, out bucket, out dummyprefix, true);
            
            if (!found)
                throw new BplusTreeException("internal tree gave bad first key");

            return bucket.FirstKey();
        }

        public TKey NextKey(TKey afterThisKey)
        {
            XBucket<TKey, byte[]> bucket;
            TKey prefix;

            if (TryFindBucketForPrefix(afterThisKey, out bucket, out prefix, false))
            {
                var result = bucket.NextKey(afterThisKey);
                if (result != null)
                    return result;
            }

            // otherwise look in the next bucket
            var nextprefix = Tree.NextKey(prefix);
            if (nextprefix == default(TKey))
                return default(TKey);

            byte[] databytes = Tree[nextprefix];
            bucket = new XBucket<TKey, byte[]>(this);
            bucket.Load(databytes);

            if (bucket.Count < 1)
                throw new BplusTreeException("empty bucket loaded");

            return bucket.FirstKey();
        }

        public bool ContainsKey(TKey key)
        {
            XBucket<TKey, byte[]> bucket;
            TKey prefix;

            if (!TryFindBucketForPrefix(key, out bucket, out prefix, false))
                return false;

            byte[] map;
            return bucket.Find(key, out map);
        }

        public byte[] this[TKey key]
        {
            get
            {
                XBucket<TKey, byte[]> bucket;
                TKey prefix;

                if (!TryFindBucketForPrefix(key, out bucket, out prefix, false))
                    throw new BplusTreeKeyMissingException("no such key in tree");

                byte[] map;

                bucket.Find(key, out map);

                return map;
            }
            set 
            {
                XBucket<TKey, byte[]> bucket;
                TKey prefix;
                if (!TryFindBucketForPrefix(key, out bucket, out prefix, false))
                {
                    bucket = new XBucket<TKey, byte[]>(this);
                }
                bucket.Add(key, value);

                Tree[prefix] = bucket.Dump();
            
            }
        }

        public IConverter<TKey, byte[]> KeyConverter { get; set; }

        public void Commit()
        {
            Tree.Commit();
        }

        public void Abort()
        {
            Tree.Abort();
        }

        public void SetFootPrintLimit(int limit)
        {
            Tree.SetFootPrintLimit(limit);
        }

        public void Shutdown()
        {
            Tree.Shutdown();
        }

        #endregion

        public void LimitBucketSize(int limit)
        {
            BucketSizeLimit = limit;
        }

        public static XBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, nodesize, buffersize, keyConverter), 
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, keyConverter), 
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, keyConverter),
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, nodesize, buffersize, keyConverter),
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, keyConverter),
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            return
                new XBplusTreeBytes<TKey>(
                    BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, keyConverter),
                    prefixLength
                    );
        }

        public static XBplusTreeBytes<TKey> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReOpen(treefile, blockfile, keyConverter);
            var prefixLength = tree.MaxKeyLength();
            return new XBplusTreeBytes<TKey>(tree, prefixLength);
        }

        public static XBplusTreeBytes<TKey> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            BplusTreeBytes<TKey> tree = BplusTreeBytes<TKey>.ReOpen(treefileName, blockfileName, keyConverter);
            int prefixLength = tree.MaxKeyLength();
            return new XBplusTreeBytes<TKey>(tree, prefixLength);
        }

        public static XBplusTreeBytes<TKey> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            BplusTreeBytes<TKey> tree = BplusTreeBytes<TKey>.ReadOnly(treefileName, blockfileName, keyConverter);
            int prefixLength = tree.MaxKeyLength();
            return new XBplusTreeBytes<TKey>(tree, prefixLength);
        }

        public virtual TKey PrefixForByteCount(TKey key, int maxbytecount)
        {
            return default(TKey);
        }

        //{
        //    if (key.Length < 1)
        //    {
        //        return default(TKey);
        //    }
        //    var prefixcharcount = maxbytecount;
        //    if (prefixcharcount > key.Length)
        //    {
        //        prefixcharcount = key.Length;
        //    }
        //    var encode =
        //        Encoding.Default.GetEncoder();
        //        //Encoding.UTF8.GetEncoder();
        //    var chars = key.ToCharArray(0, prefixcharcount);
        //    long length = encode.GetByteCount(chars, 0, prefixcharcount, true);
        //    while (length > maxbytecount)
        //    {
        //        prefixcharcount--;
        //        length = encode.GetByteCount(chars, 0, prefixcharcount, true);
        //    }
        //    return key.Substring(0, prefixcharcount);
        //}

        public bool TryFindBucketForPrefix(TKey key, out XBucket<TKey, byte[]> bucket, out TKey prefix, bool keyIsPrefix)
        {
            bucket = null;
            prefix = key;

            if (!keyIsPrefix)
                prefix = PrefixForByteCount(key, PrefixLength);

            byte[] databytes;
            if(!Tree.ContainsKey(prefix, out databytes))
                return false;

            if (default(byte[]) == databytes)
                return false; // default
                
            bucket = new XBucket<TKey, byte[]>(this);
            bucket.Load(databytes);
            if (bucket.Count < 1)
                throw new BplusTreeException("empty bucket loaded");

            return true;
        }

        public override string ToString()
        {
            return Tree.ToString();
        }
    }
}