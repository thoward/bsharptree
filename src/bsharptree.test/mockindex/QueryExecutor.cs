using System;
using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public class QueryExecutor
    {
        public QueryExecutor(IEnumerable<Term> allTerms, IEnumerable<Document> docs)
        {
            _allTerms = allTerms;
            _results = docs;
        }

        public QueryExecutor(IEnumerable<Term> allTerms)
            : this(allTerms, new List<Document>())// allTerms.Documents())
        {
        }

        private readonly IEnumerable<Term> _allTerms;

        private IEnumerable<Document> _results;
        public IEnumerable<Document> Documents()
        {
            return _results;
        }

        public QueryExecutor Should(string term)
        {
            _results = _results.Should(_allTerms, term);
            return this;
        }
        public QueryExecutor MustNot(string term)
        {
            _results = _results.MustNot(_allTerms, term);
            return this;
        }
        public QueryExecutor MustHave(string term)
        {
            _results = _results.MustHave(_allTerms, term);
            return this;
        }

        public QueryExecutor Should(QueryExecutor clause)
        {
            Console.Out.WriteLine("should have clause");
            _results = _results.Should(clause.Documents());
            return this;
        }
        public QueryExecutor MustNot(QueryExecutor clause)
        {
            Console.Out.WriteLine("must not have clause");
            _results = _results.MustNot(clause.Documents());
            return this;
        }
        public QueryExecutor MustHave(QueryExecutor clause)
        {
            Console.Out.WriteLine("must have clause");
            _results = _results.MustHave(clause.Documents());
            return this;
        }
    }
}