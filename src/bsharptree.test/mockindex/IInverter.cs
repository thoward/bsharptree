using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public interface IInverter<TSource, TUnit>
    {
        IEnumerable<IInversionUnit<TInvertableKey, TUnit>> Invert<TInvertableKey>(IInvertable<TInvertableKey, TSource, TUnit> intervable);
    }
}