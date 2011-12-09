namespace bsharptree.example.simpleindex
{
    public struct Span
    {
        public long Start;
        public long End;
        public long Length { get { return End - Start; } }
    }
}