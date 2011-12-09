using System;
using System.Collections.Generic;
using System.Linq;

namespace bsharptree.test.mockindex
{
    public class SimpleIndex : IIndex<int, string, string>
    {
        public IEnumerable<IInversion<int, string, string>> Inversions 
        { 
            get
            {
                return _terms.Values;
            }
        }

        public void AddItem(IInvertable<int, string, string> item)
        {
            AddItem(item, new StringInverter());
        }

        public void AddItem(IInvertable<int, string, string> item, IInverter<string, string> inverter)
        {
            foreach(var unit in inverter.Invert(item))
            {
                IInversion<int, string, string> term;
                if (!_terms.TryGetValue(unit.Value, out term))
                {
                    term = new Term { Value = unit.Value };
                    _terms.Add(unit.Value, term);
                }

                if (!term.Invertables.Contains(item, DocumentComparer.Default))
                    term.Invertables.Add(item);
                
            }
        }

        private Dictionary<string, IInversion<int, string, string>> _terms = new Dictionary<string, IInversion<int, string, string>>();
        
        public QueryExecutor QueryExecutor { get { return new QueryExecutor(Inversions); } }

        public QueryExecutor GetQueryExecutor(QueryClause clause)
        {
            return GetQueryExecutor(QueryExecutor, clause);
        }

        public QueryExecutor GetQueryExecutor(QueryExecutor state, QueryClause clause)
        {
            var clauseExecutor = state;

            if (clause.MustTerms.Count == 0 && clause.MustSubClauses.Count == 0)
            {
                clauseExecutor = new QueryExecutor(Inversions, new List<Document>());



                foreach (var element in clause.ShouldTerms)
                    clauseExecutor = clauseExecutor.Should(element);

                foreach (var element in clause.ShouldSubClauses)
                    clauseExecutor = clauseExecutor.Should(GetQueryExecutor(clauseExecutor, element));


                //state = AggregateQueryOperations(clause.ShouldTerms, state, state.Should);

                //state = clause.ShouldTerms.Aggregate(state, (current, term) => current.Should(term));
                //state = clause.ShouldSubClauses.Aggregate(state, (current, subclause) => current.Should(GetQueryExecutor(current, subclause)));
            }
            else
            {
                clauseExecutor = new QueryExecutor(Inversions, Inversions.Documents());


                foreach (var element in clause.MustTerms)
                    clauseExecutor = clauseExecutor.MustHave(element);

                foreach (var element in clause.MustSubClauses)
                    clauseExecutor = clauseExecutor.MustHave(GetQueryExecutor(clauseExecutor, element));

                //state = clause.MustTerms.Aggregate(state, (current, term) => current.MustHave(term));
                //state = clause.MustSubClauses.Aggregate(state, (current, subclause) => current.MustHave(GetQueryExecutor(current, subclause)));
            }

            foreach (var element in clause.MustNotTerms)
                clauseExecutor = clauseExecutor.MustNot(element);

            foreach (var element in clause.MustNotSubClauses)
                clauseExecutor = clauseExecutor.MustNot(GetQueryExecutor(clauseExecutor, element));

            //state = clause.MustNotTerms.Aggregate(state, (current, term) => current.MustNot(term));
            //state = clause.MustNotSubClauses.Aggregate(state, (current, subclause) => current.MustNot(GetQueryExecutor(current, subclause)));

            switch (clause.Flag)
            {
                case QueryClauseFlag.Should:
                    state = state.Should(clauseExecutor);
                    break;
                case QueryClauseFlag.Must:
                    state = state.MustHave(clauseExecutor);
                    break;
                case QueryClauseFlag.MustNot:
                    state = state.MustNot(clauseExecutor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return state;

        }

        // hmmm...
        public QueryExecutor AggregateQueryOperations<T>(IEnumerable<T> queryElements, QueryExecutor state, Func<T, QueryExecutor> queryOperation)
        {
            return queryElements.Aggregate(state, (current, term) => queryOperation(term));
        }
    }
}