namespace bsharptree.example.simpledb
{
    public interface IDataStore<TKey, TValue> : IDataStoreReader<TKey, TValue>, IDataStoreWriter<TKey, TValue>
    {
    }
}