using System;

namespace bsharptree.exception
{
    /// <summary>
    /// Key cannot be null or too large.
    /// </summary>
    public class BplusTreeBadKeyValueException: ApplicationException 
    {
        public BplusTreeBadKeyValueException(string message): base(message) 
        {
            // do nothing extra
        }
    }
}