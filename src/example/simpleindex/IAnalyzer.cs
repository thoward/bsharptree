namespace bsharptree.example.simpleindex
{
    using System.Collections.Generic;
    using System.IO;

    public interface IAnalyzer
    {
        IEnumerable<TermLocation> GetTermPositions(Stream stream);
    }
}