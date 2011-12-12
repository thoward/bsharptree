namespace bsharptree.example.simpleindex.query
{
    using System;
    using System.Linq;

    using bsharptree.definition;
    using bsharptree.example.simpleindex.analysis;
    using bsharptree.example.simpleindex.query.parser;

    public class QueryParser<TUnit, TSource> : IQueryParser<TUnit>
    {
        public QueryParser(IInverter<TSource, TUnit> inverter, IConverter<string, TUnit> converter, IConverter<string, TSource> phraseConverter)
        {
            _inverter = inverter;
            _converter = converter;
            _phraseConverter = phraseConverter;
        }

        private readonly IInverter<TSource, TUnit> _inverter;
        private readonly IConverter<string, TUnit> _converter;
        private readonly IConverter<string, TSource> _phraseConverter;

        public IQueryClause<TUnit> Parse(string queryText)
        {
            var parser = new Parser(new Scanner());
            var tree = parser.Parse(queryText);

            var rootExpressionNode =
                tree.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(
                    a => a.Token.Type == TokenType.Expression);

            if (default(ParseNode) == rootExpressionNode)
                throw new Exception("No query in parse tree.");

            return AnalyzeQueryNode(rootExpressionNode, _inverter, _converter, _phraseConverter);
        }

        private static IQueryClause<TUnit> AnalyzeQueryNode(ParseNode node, IInverter<TSource, TUnit> inverter, IConverter<string, TUnit> converter, IConverter<string, TSource> phraseConverter, QueryClauseFlag flag = QueryClauseFlag.Should)
        {
            // assume we are working with an expression node.
            if (node.Token.Type != TokenType.Expression)
                throw new Exception("Must be an expression node!");

            var queryClause = new QueryClause<TUnit> { Flag = flag };
            var precedingOperator = "OR";

            for (int i = 0; i < node.Nodes.Count; i++)
            {
                var subnode = node.Nodes[i];

                switch (subnode.Token.Type)
                {
                    case TokenType.MustClause:
                    case TokenType.MustNotClause:
                    case TokenType.Clause:

                        var followingOperator = GetFollowingOperator(node, i);
                        var queryClauseFlag = GetQueryClauseFlag(subnode, precedingOperator, followingOperator);

                        // determine if the clause is a term or subclause
                        foreach (var childnode in subnode.Nodes)
                        {
                            switch (childnode.Token.Type)
                            {
                                case TokenType.SubClause:
                                    var expressionNode =
                                        childnode.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Expression);

                                    if (expressionNode != default(ParseNode))
                                    {
                                        var subClause = AnalyzeQueryNode(expressionNode, inverter, converter, phraseConverter, queryClauseFlag);
                                        queryClause.AddSubClause(subClause);
                                    }
                                    break;
                                case TokenType.Term:
                                    var termNode =
                                        childnode.Nodes.FirstOrDefault(
                                            a => a.Token.Type == TokenType.TERM || a.Token.Type == TokenType.QUOTEDTERM);

                                    if (termNode != default(ParseNode))
                                    {
                                        var nodeText = termNode.Token.Text;
                                        var unitFromText = converter.From(nodeText );
                                        if (termNode.Token.Type == TokenType.QUOTEDTERM)
                                        {
                                            var sourceFromText = phraseConverter.From(nodeText);
                                            var units = inverter.Invert(sourceFromText);
                                            foreach (var unit in units) queryClause.AddUnit(unit, queryClauseFlag);
                                        }
                                        else
                                        {
                                            queryClause.AddUnit(inverter.NormalizeUnit(unitFromText), queryClauseFlag);
                                        }
                                    }

                                    break;
                            }
                        }
                        break;
                    case TokenType.OPERATOR:
                        precedingOperator = subnode.Token.Text;
                        break;
                }
            }
            return queryClause;
        }

        private static string GetFollowingOperator(ParseNode node, int i)
        {
            if ((i + 1) < node.Nodes.Count)
            {
                var followingNode = node.Nodes[i + 1];
                if (followingNode.Token.Type == TokenType.OPERATOR)
                    return followingNode.Token.Text;
            }

            return "OR"; // default
        }

        private static QueryClauseFlag GetQueryClauseFlag(ParseNode subnode, string precedingOperator, string followingOperator)
        {
            // note: +/- modifiers have higher precedence than conjunction operators, 
            // and the conjunction operator closest to the clause wins. AND OR AND NOT foo == NOT foo
            switch (subnode.Token.Type)
            {
                case TokenType.MustClause:
                    return QueryClauseFlag.Must;
                case TokenType.MustNotClause:
                    return QueryClauseFlag.MustNot;
                case TokenType.Clause:
                    if (precedingOperator == "NOT")
                        return QueryClauseFlag.MustNot;

                    // note: this causes preceding operator to have higher precedence than following
                    if (precedingOperator == "AND" || followingOperator == "AND")
                        return QueryClauseFlag.Must;

                    break;
            }
            return QueryClauseFlag.Should;
        }
    }
}