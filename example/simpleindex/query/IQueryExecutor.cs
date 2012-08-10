namespace bsharptree.example.simpleindex.query
{
    using System.Collections.Generic;

    using bsharptree.example.simpleindex.analysis;

    public interface IQueryExecutor<TKey, TSource, TUnit>
    {
        IEnumerable<IInvertable<TKey, TSource, TUnit>> Invertables();

        IQueryExecutor<TKey, TSource, TUnit> Should(TUnit term);

        IQueryExecutor<TKey, TSource, TUnit> MustNot(TUnit term);

        IQueryExecutor<TKey, TSource, TUnit> MustHave(TUnit term);

        IQueryExecutor<TKey, TSource, TUnit> Should(IQueryExecutor<TKey, TSource, TUnit> clause);

        IQueryExecutor<TKey, TSource, TUnit> MustNot(IQueryExecutor<TKey, TSource, TUnit> clause);

        IQueryExecutor<TKey, TSource, TUnit> MustHave(IQueryExecutor<TKey, TSource, TUnit> clause);

    }
}