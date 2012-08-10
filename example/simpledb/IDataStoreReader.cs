using System;
using System.Collections.Generic;

namespace bsharptree.example.simpledb
{
    public interface IDataStoreReader<TKey, TValue> : IDisposable
    {
        TValue Get(TKey key);
        TValue Get(long recordHandle);
        CompactStatistics GetCompactStatistics();
        IEnumerable<TKey> EnumerateKeys();
    }
}