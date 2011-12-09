namespace bsharptree.example.simpleindex.query
{
    using System.Linq.Expressions;

    public class TermQuery : Query<Term>
    {
        public TermQuery(QueryProvider provider)
            : base(provider)
        {
        }

        public TermQuery(QueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }
}