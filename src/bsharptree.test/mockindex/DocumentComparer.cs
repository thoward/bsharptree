using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public class DocumentComparer : IEqualityComparer<IInvertable<int, string, string>>
    {
        public static DocumentComparer Default = new DocumentComparer();
        public bool Equals(IInvertable<int, string, string> x, IInvertable<int, string, string> y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(IInvertable<int, string, string> obj)
        {
            return obj == null ? default(int) : obj.Id.GetHashCode();
        }
    }
}