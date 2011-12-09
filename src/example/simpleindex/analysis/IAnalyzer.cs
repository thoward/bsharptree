using System.Collections.Generic;
using System.IO;

namespace bsharptree.example.simpleindex.analysis
{
    public interface IAnalyzer
    {
        IEnumerable<TermLocation> GetTermPositions(Stream stream);
    }
}