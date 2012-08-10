namespace bsharptree.example.simpleindex.query
{
    using System.Collections.Generic;

    public class QueryClause<TUnit> : IQueryClause<TUnit>
    {
        public QueryClause()
        {
            Must = new List<TUnit>();
            MustNot = new List<TUnit>();
            Should = new List<TUnit>(); 
            MustSubClauses = new List<IQueryClause<TUnit>>();
            MustNotSubClauses = new List<IQueryClause<TUnit>>();
            ShouldSubClauses = new List<IQueryClause<TUnit>>();
        }
        public QueryClauseFlag Flag { get; set; }

        public List<TUnit> Must { get; private set; }
        public List<TUnit> MustNot { get; private set; }
        public List<TUnit> Should { get; private set; }

        public List<IQueryClause<TUnit>> MustSubClauses { get; private set; }
        public List<IQueryClause<TUnit>> MustNotSubClauses { get; private set; }
        public List<IQueryClause<TUnit>> ShouldSubClauses { get; private set; }

        public void AddUnit(TUnit unit, QueryClauseFlag flag)
        {
            switch (flag)
            {
                case QueryClauseFlag.Should:
                    if(!Should.Contains(unit))
                        Should.Add(unit);
                    break;
                case QueryClauseFlag.Must:
                    if (!Must.Contains(unit))
                        Must.Add(unit);
                    break;
                case QueryClauseFlag.MustNot:
                    if (!MustNot.Contains(unit))
                        MustNot.Add(unit);
                    break;
            }
        }

        public void AddSubClause(IQueryClause<TUnit> subClause)
        {
            switch (subClause.Flag)
            {
                case QueryClauseFlag.Should:
                    if (!ShouldSubClauses.Contains(subClause))
                        ShouldSubClauses.Add(subClause);
                    break;
                case QueryClauseFlag.Must:
                    if (!MustSubClauses.Contains(subClause))
                        MustSubClauses.Add(subClause);
                    break;
                case QueryClauseFlag.MustNot:
                    if (!MustNotSubClauses.Contains(subClause))
                        MustNotSubClauses.Add(subClause);
                    break;
            }
        }
    }
}