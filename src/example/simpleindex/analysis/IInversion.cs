namespace bsharptree.example.simpleindex.analysis
{
    using System.Collections.Generic;

    public interface IInversion<TInvertableKey, TSource, TUnit>
    {
        TUnit Key { get; set; }
        List<IInvertable<TInvertableKey, TSource, TUnit>> Invertables { get; set; }
        bool Match(TUnit unit);
    }
}