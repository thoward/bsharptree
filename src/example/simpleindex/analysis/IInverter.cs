namespace bsharptree.example.simpleindex.analysis
{
    using System.Collections.Generic;

    public interface IInverter<TSource, TUnit>
    {
        IEnumerable<IInversionUnit<TInvertableKey, TUnit>> Invert<TInvertableKey>(IInvertable<TInvertableKey, TSource, TUnit> intervable);
        IEnumerable<TUnit> Invert(TSource source);

        TUnit NormalizeUnit(TUnit unit);
    }
}