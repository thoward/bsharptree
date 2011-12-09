using System;
using System.IO;
using System.Text;

namespace bsharptree.example.simpleindex
{
    public struct DocumentLocation
    {
        public Span Span;
        public Guid Document;
    }
}