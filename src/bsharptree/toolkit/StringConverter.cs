using System.Text;

namespace bsharptree.toolkit
{
    public class StringConverter : GenericConverter<string, byte[]>
    {
        public static GenericConverter<string, byte[]> Default = new StringConverter();

        public StringConverter() : this(Encoding.UTF8) { }

        public StringConverter(Encoding encoding) : base(a => encoding.GetString(a), a => encoding.GetBytes(a)) { }
    }
}