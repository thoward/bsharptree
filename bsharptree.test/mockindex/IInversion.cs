using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public interface IInversion<TInvertableKey, TSource, TUnit>
    {
        TUnit Value { get; set; }
        List<IInvertable<TInvertableKey, TSource, TUnit>> Invertables { get; set; }
    }
}