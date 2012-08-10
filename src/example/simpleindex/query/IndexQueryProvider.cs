using bsharptree.example.simpleindex.analysis;

namespace bsharptree.example.simpleindex.query
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class IndexQueryProvider : QueryProvider
    {
        public IndexQueryProvider()
        {
            Terms = new TermQuery(this);
        }
        public TermQuery Terms { get; set; }

        public override string GetQueryText(Expression expression)
        {
            throw new NotImplementedException();
        }

        public override object Execute(System.Linq.Expressions.Expression expression)
        {
            switch (expression.NodeType)
            {
                //case ExpressionType.Add:
                //    break;
                //case ExpressionType.AddChecked:
                //    break;
                //case ExpressionType.And:
                //    break;
                //case ExpressionType.AndAlso:
                //    break;
                //case ExpressionType.ArrayLength:
                //    break;
                //case ExpressionType.ArrayIndex:
                //    break;
                case ExpressionType.Call:
                    
                    var callExp = expression as MethodCallExpression;
                    Console.Out.WriteLine("Method: " + callExp.Method.Name);
                    Console.Out.WriteLine("Args: ");
                    foreach (var exp in callExp.Arguments) Execute(exp);

                    break;
                //case ExpressionType.Coalesce:
                //    break;
                //case ExpressionType.Conditional:
                //    break;
                case ExpressionType.Constant:
                    var constantExp = expression as ConstantExpression;
                    Console.Out.WriteLine("Constant: ");
                    Console.Out.WriteLine("   Type:" + constantExp.Type);
                    
                    if (typeof(IQueryable).IsAssignableFrom(constantExp.Type) && Terms != constantExp.Value)
                        return
                            ((IQueryable)constantExp.Value).Provider.Execute(((IQueryable)constantExp.Value).Expression);
                    break;
                //case ExpressionType.Convert:
                //    break;
                //case ExpressionType.ConvertChecked:
                //    break;
                //case ExpressionType.Divide:
                //    break;
                //case ExpressionType.Equal:
                //    break;
                //case ExpressionType.ExclusiveOr:
                //    break;
                //case ExpressionType.GreaterThan:
                //    break;
                //case ExpressionType.GreaterThanOrEqual:
                //    break;
                //case ExpressionType.Invoke:
                //    break;
                case ExpressionType.Lambda:
                    // TODO: ypu
                    break;
                //case ExpressionType.LeftShift:
                //    break;
                //case ExpressionType.LessThan:
                //    break;
                //case ExpressionType.LessThanOrEqual:
                //    break;
                //case ExpressionType.ListInit:
                //    break;
                //case ExpressionType.MemberAccess:
                //    break;
                //case ExpressionType.MemberInit:
                //    break;
                //case ExpressionType.Modulo:
                //    break;
                //case ExpressionType.Multiply:
                //    break;
                //case ExpressionType.MultiplyChecked:
                //    break;
                //case ExpressionType.Negate:
                //    break;
                //case ExpressionType.UnaryPlus:
                //    break;
                //case ExpressionType.NegateChecked:
                //    break;
                //case ExpressionType.New:
                //    break;
                //case ExpressionType.NewArrayInit:
                //    break;
                //case ExpressionType.NewArrayBounds:
                //    break;
                //case ExpressionType.Not:
                //    break;
                //case ExpressionType.NotEqual:
                //    break;
                //case ExpressionType.Or:
                //    break;
                //case ExpressionType.OrElse:
                //    break;
                //case ExpressionType.Parameter:
                //    break;
                //case ExpressionType.Power:
                //    break;
                case ExpressionType.Quote:
                    //Console.Out.WriteLine(expression.Type);
                    //Console.Out.WriteLine(expression is UnaryExpression);
                    if((expression is UnaryExpression))
                        Execute (((UnaryExpression)expression).Operand);
                    break;
                //case ExpressionType.RightShift:
                //    break;
                //case ExpressionType.Subtract:
                //    break;
                //case ExpressionType.SubtractChecked:
                //    break;
                //case ExpressionType.TypeAs:
                //    break;
                //case ExpressionType.TypeIs:
                //    break;
                //case ExpressionType.Assign:
                //    break;
                //case ExpressionType.Block:
                //    break;
                //case ExpressionType.DebugInfo:
                //    break;
                //case ExpressionType.Decrement:
                //    break;
                //case ExpressionType.Dynamic:
                //    break;
                //case ExpressionType.Default:
                //    break;
                //case ExpressionType.Extension:
                //    break;
                //case ExpressionType.Goto:
                //    break;
                //case ExpressionType.Increment:
                //    break;
                //case ExpressionType.Index:
                //    break;
                //case ExpressionType.Label:
                //    break;
                //case ExpressionType.RuntimeVariables:
                //    break;
                //case ExpressionType.Loop:
                //    break;
                //case ExpressionType.Switch:
                //    break;
                //case ExpressionType.Throw:
                //    break;
                //case ExpressionType.Try:
                //    break;
                //case ExpressionType.Unbox:
                //    break;
                //case ExpressionType.AddAssign:
                //    break;
                //case ExpressionType.AndAssign:
                //    break;
                //case ExpressionType.DivideAssign:
                //    break;
                //case ExpressionType.ExclusiveOrAssign:
                //    break;
                //case ExpressionType.LeftShiftAssign:
                //    break;
                //case ExpressionType.ModuloAssign:
                //    break;
                //case ExpressionType.MultiplyAssign:
                //    break;
                //case ExpressionType.OrAssign:
                //    break;
                //case ExpressionType.PowerAssign:
                //    break;
                //case ExpressionType.RightShiftAssign:
                //    break;
                //case ExpressionType.SubtractAssign:
                //    break;
                //case ExpressionType.AddAssignChecked:
                //    break;
                //case ExpressionType.MultiplyAssignChecked:
                //    break;
                //case ExpressionType.SubtractAssignChecked:
                //    break;
                //case ExpressionType.PreIncrementAssign:
                //    break;
                //case ExpressionType.PreDecrementAssign:
                //    break;
                //case ExpressionType.PostIncrementAssign:
                //    break;
                //case ExpressionType.PostDecrementAssign:
                //    break;
                //case ExpressionType.TypeEqual:
                //    break;
                //case ExpressionType.OnesComplement:
                //    break;
                //case ExpressionType.IsTrue:
                //    break;
                //case ExpressionType.IsFalse:
                //    break;
                default:
                    Console.Out.WriteLine(expression.GetType());
                    Console.Out.WriteLine(expression.NodeType);
                    break;
            }
            return new List<DocumentLocation>();
        }
    }
}