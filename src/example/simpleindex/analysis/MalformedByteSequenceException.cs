using System;

namespace bsharptree.example.simpleindex.analysis
{
    public class MalformedByteSequenceException : Exception
    {
        public MalformedByteSequenceException(string s) : base(s)
        {
        }
    }
}