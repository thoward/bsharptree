using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public interface IIndex<TKey, TSource, TUnit>
    {
        IEnumerable<IInversion<TKey, TSource, TUnit>> Inversions { get; }

        void AddItem(IInvertable<TKey, TSource, TUnit> item, IInverter<TSource, TUnit> inverter);
    }
}