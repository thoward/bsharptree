using System;
using System.Collections.Generic;
using System.Linq;
using bsharptree.definition;
using bsharptree.exception;
using bsharptree.toolkit;

namespace bsharptree
{
    /// <summary>
    /// Bucket for elements with same prefix -- designed for small buckets.
    /// </summary>
    public class XBucket<TKey, TValue>
        where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        private readonly List<TKey> _keys;
        private readonly List<TValue> _values;
        private readonly XBplusTreeBytes<TKey> _owner;

        public XBucket(XBplusTreeBytes<TKey> owner)
        {
            _keys = new List<TKey>();
            _values = new List<TValue>();
            _owner = owner;
        }

        public int Count 
        {   
            get {
                return _keys.Count;
            }
        }

        public IConverter<TValue, byte[]> ValueConverter { get; set; }
        public IConverter<TKey, byte[]> KeyConverter { get; set; }

        public void Load(byte[] serialization)
        {
            var index = 0;
            var byteCount = serialization.Length; 
            if (_values.Count != 0 || _keys.Count != 0)
                throw new BplusTreeException("load into nonempty xBucket not permitted");

            while (index < byteCount)
            {
                // get key prefix and key
                var keylength = ByteTools.Retrieve(serialization, index);
                index += ByteTools.IntStorage;

                var keybytes = new byte[keylength];
                Buffer.BlockCopy(serialization, index, keybytes, 0, keylength);
                
                var keystring =
                    KeyConverter.To(keybytes);
                    //StringTools.BytesToString(keybytes);

                index += keylength;

                // get value prefix and value
                var valuelength = ByteTools.Retrieve(serialization, index);
                index += ByteTools.IntStorage;
                
                var valuebytes = new byte[valuelength];
                Buffer.BlockCopy(serialization, index, valuebytes, 0, valuelength);

                // record new key and value
                _keys.Add(keystring);
                _values.Add(ValueConverter.To(valuebytes));
                
                index += valuelength;
            }

            if (index != byteCount)
                throw new BplusTreeException("bad byte count in serialization " + byteCount);
        }

        public byte[] Dump()
        {
            var allbytes = new List<byte[]>();
            
            for (var index = 0; index < _keys.Count; index++)
            {
                var thisKey = _keys[index];
                var thisValue = _values[index];
                
                var keyprefix = new byte[ByteTools.IntStorage];
                var keybytes = KeyConverter.From(thisKey);
                    //StringTools.StringToBytes(thisKey);
                
                ByteTools.Store(keybytes.Length, keyprefix, 0);
                
                allbytes.Add(keyprefix);
                allbytes.Add(keybytes);

                var valueprefix = new byte[ByteTools.IntStorage];
                var valueBytes = ValueConverter.From(thisValue);

                ByteTools.Store(valueBytes.Length, valueprefix, 0);
                
                allbytes.Add(valueprefix);
                allbytes.Add(valueBytes);
            }

            var byteCount = allbytes.Sum(thebytes => thebytes.Length);
            var outindex = 0;
            var result = new byte[byteCount];

            foreach (var thebytes in allbytes)
            {
                Buffer.BlockCopy(thebytes, 0, result, outindex, thebytes.Length);
                outindex += thebytes.Length; 
            }

            if (outindex != byteCount)
                throw new BplusTreeException("error counting bytes in dump " + outindex + "!=" + byteCount);

            return result;
        }

        public void Add(TKey key, byte[] map)
        {
            var index = 0;
            var limit = _owner.BucketSizeLimit;

            while (index < _keys.Count)
            {
                var thiskey = _keys[index];

                var comparison = 
                    thiskey != null 
                    ? thiskey.CompareTo(key) 
                    : key == null 
                        ? 0 
                        : -1;

                if (comparison == 0)
                {
                    _values[index] = ValueConverter.To(map);
                    _keys[index] = key;
                    return;
                }
                
                if (comparison > 0)
                {
                    _values.Insert(index, ValueConverter.To(map));
                    _keys.Insert(index, key);
                    
                    if (limit > 0 && _keys.Count > limit)
                        throw new BplusTreeBadKeyValueException("bucket size limit exceeded");

                    return;
                }

                index++;
            }

            _keys.Add(key);
            _values.Add(ValueConverter.To(map));

            if (limit > 0 && _keys.Count > limit)
                throw new BplusTreeBadKeyValueException("bucket size limit exceeded");
        }

        public void Remove(TKey key)
        {
            var index = 0;
            while (index < _keys.Count)
            {
                var thiskey = _keys[index];
                if (thiskey != null && thiskey.CompareTo(key) == 0)
                {
                    _values.RemoveAt(index);
                    _keys.RemoveAt(index);
                    return;
                }
                index++;
            }

            throw new BplusTreeBadKeyValueException("cannot remove missing key: " + key);
        }

        public bool Find(TKey key, out byte[] map)
        {
            map = null;
            int index = 0;
            while (index < _keys.Count)
            {
                var thiskey = _keys[index];
                if (thiskey != null && thiskey.CompareTo(key) == 0)
                {
                    map = ValueConverter.From(_values[index]);
                    return true;
                }
                index++;
            }
            return false;
        }

        public TKey FirstKey()
        {
            return _keys.Count < 1 
                ? default(TKey) 
                : _keys[0];
        }

        public TKey NextKey(TKey afterThisKey)
        {
            var index = 0;
            
            while (index < _keys.Count)
            {
                var thiskey = _keys[index];

                if (thiskey != null && thiskey.CompareTo(afterThisKey) > 0)
                    return thiskey;

                index++;
            }

            return default(TKey);
        }
    }
}