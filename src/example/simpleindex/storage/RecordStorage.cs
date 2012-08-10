namespace bsharptree.example.simpleindex.storage
{
    using System.IO;

    public abstract class RecordStorage<TValue>
    {
        protected readonly object _streamLock = new object();

        protected Stream _stream;

        protected RecordStorage(Stream stream)
        {
            _stream = stream;
        }

        public abstract long Add(TValue locations);

        public abstract TValue Get(long offset);
    }
}