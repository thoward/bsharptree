namespace bsharptree.example.simpleindex.storage
{
    using bsharptree.example.simpleindex.analysis;

    public interface IIndexWriter<TKey, TSource, TUnit>
    {
        void AddItem(IInvertable<TKey, TSource, TUnit> item, IInverter<TSource, TUnit> inverter);
    }
}