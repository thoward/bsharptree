namespace bsharptree.example.simpleindex.storage
{
    public interface IStorageItem<TKey, TValue>
    {
        TKey Key { get; }

        TValue Value { get; }
    }
}