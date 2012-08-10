namespace bsharptree.example.simpleindex.query
{
    using System.Collections.Generic;

    using bsharptree.example.simpleindex.analysis;

    public interface IIndexReader<TKey, TSource, TUnit> 
    {
        IEnumerable<IInversion<TKey, TSource, TUnit>> Inversions { get; }

        IQueryExecutor<TKey, TSource, TUnit> GetQueryExecutor(IQueryClause<TUnit> clause);
        IQueryExecutor<TKey, TSource, TUnit> GetQueryExecutor(IQueryExecutor<int, string, string> state, IQueryClause<string> clause);

        IEnumerable<IInvertable<int, string, string>> ExecuteQuery(string queryText, IInverter<string, string> inverter);
    }
}