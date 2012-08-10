using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public class TermComparer : IEqualityComparer<Term>
    {
        public static TermComparer Default = new TermComparer();
        public bool Equals(Term x, Term y)
        {
            if (x == null && y == null) return true;
            if (x == null || y==null) return false;

            return x.Value.Equals(y.Value);
        }

        public int GetHashCode(Term obj)
        {
            return obj == null ? default(int) : obj.Value.GetHashCode();
        }
    }
}