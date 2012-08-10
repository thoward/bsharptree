using System;

namespace bsharptree.exception
{
    /// <summary>
    /// No such key found for attempted retrieval.
    /// </summary>
    public class BplusTreeKeyMissingException: ApplicationException 
    {
        public BplusTreeKeyMissingException(string message): base(message) 
        {
            // do nothing extra
        }
    }
}