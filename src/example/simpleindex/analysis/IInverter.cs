namespace bsharptree.example.simpleindex.analysis
{
    using System.Collections.Generic;

    public interface IInverter<TInvertableKey, TSource, TUnit>
    {
        IEnumerable<IInversionUnit<TInvertableKey, TUnit>> Invert(IInvertable<TInvertableKey, TSource, TUnit> intervable);
        IEnumerable<TUnit> Invert(TSource source);

        TUnit NormalizeUnit(TUnit unit);
    }
}