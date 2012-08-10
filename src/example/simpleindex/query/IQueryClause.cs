namespace bsharptree.example.simpleindex.query
{
    using System.Collections.Generic;

    public interface IQueryClause<TUnit>
    {
        QueryClauseFlag Flag { get; set; }

        List<TUnit> Must { get; }
        List<TUnit> MustNot { get; }
        List<TUnit> Should { get; }

        List<IQueryClause<TUnit>> MustSubClauses { get; }
        List<IQueryClause<TUnit>> MustNotSubClauses { get; }
        List<IQueryClause<TUnit>> ShouldSubClauses { get; }

        void AddUnit(TUnit unit, QueryClauseFlag flag);

        void AddSubClause(IQueryClause<TUnit> subClause);
    }
}