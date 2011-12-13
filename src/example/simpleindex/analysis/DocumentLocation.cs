using System;

namespace bsharptree.example.simpleindex.analysis
{
    public struct DocumentLocation : IInversionUnit<Guid, TermLocation>
    {
        public Guid InvertableKey { get; set; }
        public TermLocation Unit { get; set; }
    }
}