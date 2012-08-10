using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    using bsharptree.example.simpleindex.analysis;

    public class Term : IInversion<int, string, string>
    {
        public Term()
        {
            Key = string.Empty;
            Invertables = new List<IInvertable<int, string, string>>();
        }
        
        public string Key { get; set; }
        public List<IInvertable<int, string, string>> Invertables { get; set; }
    }
}