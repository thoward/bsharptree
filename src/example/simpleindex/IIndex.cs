namespace bsharptree.test.mockindex
{
    using System.Collections.Generic;

    using bsharptree.example.simpleindex.query;
    using bsharptree.example.simpleindex.storage;

    public interface IIndex<TKey, TSource, TUnit> : IIndexReader<TKey, TSource, TUnit>, IIndexWriter<TKey, TSource, TUnit>
    {
    }
}