namespace bsharptree.test.mockindex
{
    public class InversionUnit<TInvertableKey, TUnit> : IInversionUnit<TInvertableKey, TUnit>
    {
        public TInvertableKey Key { get; set; }
        public TUnit Value { get; set; }
    }
}