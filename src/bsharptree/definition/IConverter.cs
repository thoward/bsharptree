using System;

namespace bsharptree.definition
{
    public interface IConverter<TSource, TDest>
    {
        Func<TDest, TSource> To { get; set; }
        Func<TSource, TDest> From { get; set; }
    }
}