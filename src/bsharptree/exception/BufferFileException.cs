using System;

namespace bsharptree.exception
{
    public class BufferFileException : ApplicationException
    {
        public BufferFileException(string message) : base(message)
        {
            // do nothing extra
        }
    }
}