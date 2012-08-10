using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public class QueryClause
    {
        public QueryClauseFlag Flag { get; set; }

        public List<string> MustTerms = new List<string>();
        public List<string> MustNotTerms = new List<string>();
        public List<string> ShouldTerms = new List<string>();

        public List<QueryClause> MustSubClauses = new List<QueryClause>();
        public List<QueryClause> MustNotSubClauses = new List<QueryClause>();
        public List<QueryClause> ShouldSubClauses = new List<QueryClause>();

        public void AddTerm(string term, QueryClauseFlag flag)
        {
            switch (flag)
            {
                case QueryClauseFlag.Should:
                    if(!ShouldTerms.Contains(term))
                        ShouldTerms.Add(term);
                    break;
                case QueryClauseFlag.Must:
                    if (!MustTerms.Contains(term))
                        MustTerms.Add(term);
                    break;
                case QueryClauseFlag.MustNot:
                    if (!MustNotTerms.Contains(term))
                        MustNotTerms.Add(term);
                    break;
            }
        }
        public void AddSubClause(QueryClause subClause)
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