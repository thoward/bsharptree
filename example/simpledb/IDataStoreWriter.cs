using System;

namespace bsharptree.example.simpledb
{
    public interface IDataStoreWriter<TKey, TValue> : IDisposable
    {
        void Save(TKey key, TValue value);

        void Delete(TKey key);

        void Compact();
    }
}