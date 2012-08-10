using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using bsharptree.definition;
using bsharptree.example.simpleindex.analysis;
using bsharptree.toolkit;

namespace bsharptree.example.simpleindex
{
    using bsharptree.example.simpleindex.storage;

    using Guid = System.Guid;

    public struct Term : IEquatable<Term>, IComparable<Term>, IStorageItem<string, IEnumerable<DocumentLocation>>, IInversion<Guid, IEnumerable<DocumentLocation>, string>
    {
        public Term(string value, IEnumerable<DocumentLocation> docLocations)
            : this()
        {
            Key = value;
            Value = docLocations;
        }

        public string Key { get; set; }

        public List<IInvertable<Guid, IEnumerable<DocumentLocation>, string>> Invertables
        {
            get
            {
                return (List<IInvertable<Guid, IEnumerable<DocumentLocation>, string>>)Value;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool Match(string unit)
        {
            return Key.Equals(unit) 
                ? true // complete match 
                : Key.StartsWith(unit); // implicit wildcards
        }

        public IEnumerable<DocumentLocation> Value { get; set; }

        public bool Equals(Term other)
        {
            return Key.Equals(other.Key);
        }

        public int CompareTo(Term other)
        {
            return Key.CompareTo(other);
        }
    }
}