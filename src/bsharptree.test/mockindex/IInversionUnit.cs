namespace bsharptree.test.mockindex
{
    public interface IInversionUnit<TInvertableKey, TUnit>
    {
        TInvertableKey Key { get; set; }
        TUnit Value { get; set; }
    }
}