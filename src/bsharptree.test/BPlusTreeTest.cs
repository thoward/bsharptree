using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using bsharptree.toolkit;
using NUnit.Framework;
using bsharptree;
using Guid = bsharptree.toolkit.Guid;

namespace bsharptree.test
{
    [TestFixture]
    public class BPlusTreeTest
    {
        /// <summary>
        /// A simple test of data in/data out using Guids for keys. Note: We have to use the wrapper in the library
        /// in this case, because System.Guid is not a reference type.
        /// </summary>
        [Test]
        public void TestBplusTreeLong()
        {
            using(var indexStream = new MemoryStream())
            {
                var tree = BplusTreeLong<Guid>
                    .InitializeInStream(
                        indexStream,
                        16, 
                        8,
                        Guid.DefaultConverter);

                
                var testValueCache = new Dictionary<Guid, long>();

                for (long i = 0; i < 100; i++)
                {
                    var key = Guid.NewGuid();
                    tree[key] = i;
                    testValueCache.Add(key, i);
                }
                
                tree.Commit();
                
                // dump tree to console for debugging
                Console.Out.WriteLine(tree.ToString());

                foreach (var testValue in testValueCache)
                {
                    Assert.AreEqual(testValue.Value, tree[testValue.Key]);
                }
            }
        }
    }
}
