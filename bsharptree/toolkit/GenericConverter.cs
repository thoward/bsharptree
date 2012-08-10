using System;
using bsharptree.definition;

namespace bsharptree.toolkit
{
    public class GenericConverter<TSource, TDest> : IConverter<TSource, TDest>
    {
        public GenericConverter(Func<TDest, TSource> to, Func<TSource, TDest> from)
        {
            To = to;
            From = from;
        }

        public Func<TDest, TSource> To { get; set; }
        public Func<TSource, TDest> From { get; set; }
    }
}