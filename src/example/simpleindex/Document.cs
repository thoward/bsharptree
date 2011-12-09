using System;
using System.IO;

namespace bsharptree.example.simpleindex
{
    using bsharptree.example.simpleindex.storage;

    public struct Document : IStorageItem<toolkit.Guid, Stream>
    {
        public Document(Guid key, Stream valueStream)
        {
            _guid = key;
            _valueStream = valueStream;
        }

        private Guid _guid;
        private readonly Stream _valueStream;

        public Guid Key
        {
            get { return _guid; }
            set
            {
                _guid = value;
            }
        }

        toolkit.Guid IStorageItem<toolkit.Guid, Stream>.Key { get { return _guid; }}
        public Stream Value { get { return _valueStream; }}
    }
}