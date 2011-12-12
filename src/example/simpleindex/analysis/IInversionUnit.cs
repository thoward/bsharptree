namespace bsharptree.example.simpleindex.analysis
{
    public interface IInversionUnit<TInvertableKey, TUnit>
    {
        TInvertableKey InvertableKey { get; set; }
        TUnit Unit { get; set; }
    }
}