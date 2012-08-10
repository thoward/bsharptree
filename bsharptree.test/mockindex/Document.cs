namespace bsharptree.test.mockindex
{
    using bsharptree.example.simpleindex.analysis;

    public class Document : IInvertable<int, string, string>
    {
        public Document()
        {
            Id = 0;
            Value = string.Empty;
        }

        public int Id { get; set; }

        public string Value { get; set; }
    }
}