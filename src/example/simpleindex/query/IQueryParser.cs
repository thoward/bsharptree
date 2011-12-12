namespace bsharptree.example.simpleindex.query
{
    public interface IQueryParser<TUnit>
    {
        IQueryClause<TUnit> Parse(string queryText);
    }
}