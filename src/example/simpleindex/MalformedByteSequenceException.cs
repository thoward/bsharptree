using System;

namespace bsharptree.example.simpleindex
{
    public class MalformedByteSequenceException : Exception
    {
        public MalformedByteSequenceException(string s) : base(s)
        {
        }
    }
}