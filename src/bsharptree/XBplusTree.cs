using System;
using System.IO;
using bsharptree.definition;
using bsharptree.io;

namespace bsharptree
{
    /// <summary>
    /// Tree index mapping strings to strings with unlimited key length
    /// </summary>
    public class XBplusTree<TKey, TValue> : BplusTree<XBplusTreeBytes<TKey>, TKey, TValue> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        private readonly XBplusTreeBytes<TKey> _xtree;

        public XBplusTree(XBplusTreeBytes<TKey> tree)
            : base(tree)
        {
            _xtree = tree;
        }

        public void LimitBucketSize(int limit)
        {
            _xtree.BucketSizeLimit = limit;
        }

        public static new XBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, nodesize, buffersize, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> Initialize(string treefileName, string blockfileName, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, nodesize, buffersize, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> Initialize(Stream treefile, Stream blockfile, int keyLength, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.Initialize(treefile, blockfile, keyLength, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.ReOpen(treefile, blockfile, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.ReOpen(treefileName, blockfileName, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }

        public static new XBplusTree<TKey, TValue> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = XBplusTreeBytes<TKey>.ReadOnly(treefileName, blockfileName, keyConverter);
            return new XBplusTree<TKey, TValue>(tree);
        }
    }
}