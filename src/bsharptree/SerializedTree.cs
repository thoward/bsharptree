using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using bsharptree.definition;

namespace bsharptree
{
    /// <summary>
    /// Wrapper for any IByteTree implementation which implements automatic object serialization/deserialization
    /// for serializable objects.
    /// </summary>
    public class SerializedTree<TKey> : ITreeIndex<TKey, object> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        private readonly BinaryFormatter _formatter;
        private readonly ITreeIndex<TKey, byte[]> _tree;

        public SerializedTree(ITreeIndex<TKey, byte[]> tree)
        {
            _formatter = new BinaryFormatter();
            _tree = tree;
        }

        #region ITreeIndex<string,object> Members

        public object this[TKey key]
        {
            get
            {
                using (var bstream = new MemoryStream(_tree[key]))
                    return _formatter.Deserialize(bstream);
            }
            set
            {
                using (var bstream = new MemoryStream())
                {
                    _formatter.Serialize(bstream, value);
                    _tree[key] = bstream.ToArray();
                }
            }
        }

        public IConverter<TKey, byte[]> KeyConverter { get; set; }
        public IConverter<object, byte[]> ValueConverter { get; set; }

        public void Recover(bool correctErrors)
        {
            _tree.Recover(correctErrors);
        }

        public void RemoveKey(TKey key)
        {
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

        public bool ContainsKey(TKey key)
        {
            return _tree.ContainsKey(key);
        }
        public bool UpdateKey(TKey key, object value)
        {
            return _tree.UpdateKey(key, ValueConverter.From(value));
        }

        public void Commit()
        {
            _tree.Commit();
        }

        public void Abort()
        {
            _tree.Abort();
        }

        public void SetFootPrintLimit(int limit)
        {
            _tree.SetFootPrintLimit(limit);
        }

        public void Shutdown()
        {
            _tree.Shutdown();
        }

        #endregion
    }
}