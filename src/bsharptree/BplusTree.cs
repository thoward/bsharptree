using System;
using System.IO;
using bsharptree.definition;
using bsharptree.io;

namespace bsharptree
{
    /// <summary>
    /// Tree index mapping keys to values.
    /// </summary>
    public class BplusTree<TWrappedTree, TKey, TValue> : ITreeIndex<TKey, TValue>
        where TWrappedTree : ITreeIndex<TKey, byte[]>
        where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        /// <summary>
        /// Internal tree mapping strings to bytes (for conversion to strings).
        /// </summary>
        public TWrappedTree Tree;

        public BplusTree(TWrappedTree tree)
        {
            Tree = tree;
        }

        #region ITreeIndex<string,string> Members

        public IConverter<TKey, byte[]> KeyConverter { get; set; }
        public IConverter<TValue, byte[]> ValueConverter { get; set; }

        public void Recover(bool correctErrors)
        {
            Tree.Recover(correctErrors);
        }

        public void RemoveKey(TKey key)
        {
            Tree.RemoveKey(key);
        }

        public TKey FirstKey()
        {
            return Tree.FirstKey();
        }

        public TKey NextKey(TKey afterThisKey)
        {
            return Tree.NextKey(afterThisKey);
        }

        public bool ContainsKey(TKey key)
        {
            return Tree.ContainsKey(key);
        }

        public bool UpdateKey(TKey key, TValue value)
        {
            return Tree.UpdateKey(key, ValueConverter.From(value));
        }

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

        public virtual TValue this[TKey key]
        {
            get
            {
                return ValueConverter.To(Tree[key]);
                //return StringTools.BytesToString(Tree[key]);
            }
            set
            {
                Tree[key] = ValueConverter.From(value);
                //Tree[key] = StringTools.StringToBytes(value);
            }
        }

        #endregion

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(string treefileName, string blockfileName, int keyLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, keyLength, cultureId, nodesize, buffersize, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(string treefileName, string blockfileName, int keyLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, keyLength, cultureId, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(string treefileName, string blockfileName, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, keyLength, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(Stream treefile, Stream blockfile, int keyLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefile, blockfile, keyLength, cultureId, nodesize, buffersize, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(Stream treefile, Stream blockfile, int keyLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefile, blockfile, keyLength, cultureId, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> Initialize(Stream treefile, Stream blockfile, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.Initialize(treefile, blockfile, keyLength, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReOpen(treefile, blockfile, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReOpen(treefileName, blockfileName, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }

        public static BplusTree<BplusTreeBytes<TKey>, TKey, TValue> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReadOnly(treefileName, blockfileName, keyConverter);
            return new BplusTree<BplusTreeBytes<TKey>, TKey, TValue>(tree);
        }
    }
}