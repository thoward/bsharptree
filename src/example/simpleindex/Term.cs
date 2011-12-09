using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using bsharptree.definition;
using bsharptree.example.simpleindex.analysis;
using bsharptree.toolkit;

namespace bsharptree.example.simpleindex
{
    using bsharptree.example.simpleindex.storage;

    public struct Term : IEquatable<Term>, IComparable<Term>, IStorageItem<string, IEnumerable<DocumentLocation>>
    {
        public Term(string value, IEnumerable<DocumentLocation> docLocations)
            : this()
        {
            Key = value;
            Value = docLocations;
        }

        public string Key { get; set; }

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