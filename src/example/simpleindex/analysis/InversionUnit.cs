namespace bsharptree.example.simpleindex.analysis
{
    public class InversionUnit<TInvertableKey, TUnit> : IInversionUnit<TInvertableKey, TUnit>
    {
        public TInvertableKey InvertableKey { get; set; }
        public TUnit Unit { get; set; }
    }
}