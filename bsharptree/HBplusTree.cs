using System;
using System.IO;
using bsharptree.definition;
using bsharptree.io;

namespace bsharptree
{
    /// <summary>
    /// Tree index mapping strings to strings with unlimited key length
    /// </summary>
    public class HBplusTree<TKey, TValue> : BplusTree<HBplusTreeBytes<TKey>, TKey, TValue> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        private readonly HBplusTreeBytes<TKey> _xtree;

        public HBplusTree(HBplusTreeBytes<TKey> tree)
            : base(tree)
        {
            _xtree = tree;
        }

        public void LimitBucketSize(int limit)
        {
            _xtree.BucketSizeLimit = limit;
        }

        public static new HBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, nodesize, buffersize, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, nodesize, buffersize, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.Initialize(treefile, blockfile, keyLength, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.ReOpen(treefile, blockfile, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.ReOpen(treefileName, blockfileName, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public static new HBplusTree<TKey, TValue> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = HBplusTreeBytes<TKey>.ReadOnly(treefileName, blockfileName, keyConverter);
            return new HBplusTree<TKey, TValue>(tree);
        }

        public override string ToString()
        {
            return Tree.ToString();
        }
    }
}