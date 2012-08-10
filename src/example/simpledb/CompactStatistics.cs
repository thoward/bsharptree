using System;

namespace bsharptree.example.simpledb
{
    public class CompactStatistics
    {
        public ulong RecordCount { get; internal set; }
        public ulong EstimatedTotalPostCompactSize { get { return EstimatedPostCompactIndexSize + PostCompactBlobSize; } }
        public ulong EstimatedPostCompactIndexSize { get; internal set; }
        public ulong PostCompactBlobSize { get; internal set; }
        public ulong TotalSize { get { return IndexSize + BlobSize; } }
        public ulong IndexSize { get; internal set; }
        public ulong BlobSize { get; internal set; }
    }
}