using System;

namespace bsharptree.exception
{
    /// <summary>
    /// Generic error including programming errors.
    /// </summary>
    public class BplusTreeException: ApplicationException 
    {
        public BplusTreeException(string message): base(message) 
        {
            // do nothing extra
        }
    }
}