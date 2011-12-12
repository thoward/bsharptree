namespace bsharptree.example.simpleindex.query
{
    using System;
    using System.Collections.Generic;

    using bsharptree.example.simpleindex.analysis;

    public class QueryExecutor<TKey, TSource, TUnit> : IQueryExecutor<TKey, TSource, TUnit>
    {
        public QueryExecutor(IEnumerable<IInversion<TKey, TSource, TUnit>> allTerms, IEnumerable<IInvertable<TKey, TSource, TUnit>> docs, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            _comparer = comparer;
            _allTerms = allTerms;
            _results = docs;
        }

        public QueryExecutor(IEnumerable<IInversion<TKey, TSource, TUnit>> allTerms, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
            : this(allTerms, new List<IInvertable<TKey, TSource, TUnit>>(), comparer)// allTerms.Documents())
        {
        }

        private readonly IEqualityComparer<IInvertable<TKey, TSource, TUnit>> _comparer;
        private readonly IEnumerable<IInversion<TKey, TSource, TUnit>> _allTerms;

        private IEnumerable<IInvertable<TKey, TSource, TUnit>> _results;

        public IEnumerable<IInvertable<TKey, TSource, TUnit>> Invertables()
        {
            return _results;
        }

        public IQueryExecutor<TKey, TSource, TUnit> Should(TUnit term)
        {
            _results = _results.Should(_allTerms, term, _comparer);
            return this;
        }

        public IQueryExecutor<TKey, TSource, TUnit> MustNot(TUnit term)
        {
            _results = _results.MustNot(_allTerms, term, _comparer);
            return this;
        }
        
        public IQueryExecutor<TKey, TSource, TUnit> MustHave(TUnit term)
        {
            _results = _results.MustHave(_allTerms, term, _comparer);
            return this;
        }

        public IQueryExecutor<TKey, TSource, TUnit> Should(IQueryExecutor<TKey, TSource, TUnit> clause)
        {
            Console.Out.WriteLine("should have clause");
            _results = _results.Should(clause.Invertables(), _comparer);
            return this;
        }

        public IQueryExecutor<TKey, TSource, TUnit> MustNot(IQueryExecutor<TKey, TSource, TUnit> clause)
        {
            Console.Out.WriteLine("must not have clause");
            _results = _results.MustNot(clause.Invertables(), _comparer);
            return this;
        }

        public IQueryExecutor<TKey, TSource, TUnit> MustHave(IQueryExecutor<TKey, TSource, TUnit> clause)
        {
            Console.Out.WriteLine("must have clause");
            _results = _results.MustHave(clause.Invertables());
            return this;
        }
    }
}