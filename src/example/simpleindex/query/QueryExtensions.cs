namespace bsharptree.example.simpleindex.query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using bsharptree.example.simpleindex.analysis;

    public static class QueryExtensions
    {
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> Documents<TKey, TSource, TUnit>(this IEnumerable<IInversion<TKey, TSource, TUnit>> results, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            Console.Out.WriteLine("docs t, ");
            return results.SelectMany(a => a.Invertables).Distinct(comparer);
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> Documents<TKey, TSource, TUnit>(this IEnumerable<IInversion<TKey, TSource, TUnit>> inversions, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer, TUnit unit)
        {
            Console.Out.WriteLine("docs " + unit + ", ");
            return inversions.Where(a =>
                {
                    //Console.Out.WriteLine("a: " + a.Value + ", b: " + unit);
                    return a.Match(unit);
                }).Documents(comparer);
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> Should<TKey, TSource, TUnit>(this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInversion<TKey, TSource, TUnit>> allTerms, TUnit unit, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            Console.Out.WriteLine("should have " + unit + ", ");
            return results.Should(allTerms.Documents(comparer, unit), comparer);
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> MustNot<TKey, TSource, TUnit>(this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInversion<TKey, TSource, TUnit>> allTerms, TUnit unit, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            Console.Out.WriteLine("must not have " + unit + ", ");
            return results.MustNot(allTerms.Documents(comparer, unit), comparer);
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> MustHave<TKey, TSource, TUnit>(this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInversion<TKey, TSource, TUnit>> allTerms, TUnit unit, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            Console.Out.WriteLine("must have " + unit + ", ");
            return results.MustHave(allTerms.Documents(comparer, unit));
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> Should<TKey, TSource, TUnit>(this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInvertable<TKey, TSource, TUnit>> other, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            //return results.Concat(other).Distinct(DocumentComparer.Default);
            return results.Union(other, comparer);
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> MustNot<TKey, TSource, TUnit>
            (this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInvertable<TKey, TSource, TUnit>> other, IEqualityComparer<IInvertable<TKey, TSource, TUnit>> comparer)
        {
            return results.Where(a=> !other.Contains(a, comparer));
        }
        public static IEnumerable<IInvertable<TKey, TSource, TUnit>> MustHave<TKey, TSource, TUnit>(this IEnumerable<IInvertable<TKey, TSource, TUnit>> results, IEnumerable<IInvertable<TKey, TSource, TUnit>> other)
        {
            //return results.Join(other, a => a.DocID, b => b.DocID, (a, b) => a);
            return results.Where(other.Contains);//.Union(other.Where(results.Contains));
        }

       
    }
}