using System;
using System.Collections.Generic;

namespace bsharptree.test.mockindex
{
    public interface IInvertable<TKey, TValue, TUnit>
    {
        TKey Id { get; set; }
        TValue Value { get; set; }
        IComparer<TKey> GetComparer();
    }

    public class Document : IInvertable<int, string, string>
    {
        public Document()
        {
            Id = 0;
            Value = string.Empty;
        }

        public int Id { get; set; }

        public string Value { get; set; }

        public IComparer<int> GetComparer()
        {
            throw new NotImplementedException();
        }
    }
}