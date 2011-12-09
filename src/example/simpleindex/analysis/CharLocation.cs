namespace bsharptree.example.simpleindex.analysis
{
    public struct CharLocation
    {
        public static CharLocation Empty;

        public Span ByteSpan;
        public char? Value;
    }
}