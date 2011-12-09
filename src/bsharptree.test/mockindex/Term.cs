using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public class Term : IInversion<int, string, string>
    {
        public Term()
        {
            Value = string.Empty;
            Invertables = new List<IInvertable<int, string, string>>();
        }
        
        public string Value { get; set; }
        public List<IInvertable<int, string, string>> Invertables { get; set; }
    }
}