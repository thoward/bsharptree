namespace bsharptree.example.simpleindex.analysis
{
    public struct Span
    {
        public long Start;
        public long End;
        public long Length { get { return End - Start; } }
    }
}