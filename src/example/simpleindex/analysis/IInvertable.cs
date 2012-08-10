namespace bsharptree.example.simpleindex.analysis
{
    public interface IInvertable<TKey, TValue, TUnit>
    {
        TKey Id { get; set; }
        TValue Value { get; set; }
    }
}