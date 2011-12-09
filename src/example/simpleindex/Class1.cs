using System;
using System.IO;
using System.Linq;

namespace bsharptree.example.simpleindex
{
    using bsharptree.example.simpleindex.query;
    using bsharptree.example.simpleindex.storage;

    public class MockClient
    {
        public void DoQuery()
        {
            var foo = new IndexQueryProvider();
            var docStorage = new DocumentStorage(new MemoryStream(), new MemoryStream());

            var results = from term in foo.Terms
                          from location in term.Value
                          where term.Key == "foo" || term.Key == "bar"
                          select location;

            foreach (var result in results.Distinct())
            {
                Console.Out.WriteLine(result.Document);
                var document = docStorage.Get(result.Document);
                var context = docStorage.GetContext(result);
                Console.Out.WriteLine(context);
                Console.Out.WriteLine();
            }
        }
    }
}