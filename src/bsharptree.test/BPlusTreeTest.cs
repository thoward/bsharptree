using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using bsharptree.example.simpledb;
using bsharptree.example.simpleindex.analysis;
using bsharptree.test.mockindex;
using bsharptree.toolkit;
using NUnit.Framework;
using bsharptree;
using Guid = bsharptree.toolkit.Guid;

namespace bsharptree.test
{
    using bsharptree.example.simpleindex;
    using bsharptree.example.simpleindex.query;
    using bsharptree.example.simpleindex.query.parser;
    using bsharptree.example.simpleindex.storage;

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

        [Test]
        public void TestSimpleDB()
        {
            string indexDirectory = @"C:\test-simple-db\1\";
            var keys = new List<Guid>();
            int iterations = 10000;
            Console.Out.WriteLine("building");
            var watch = new Stopwatch();
            watch.Start();
            using(var db = new GuidStore(indexDirectory))
            {
                for (int i = 0; i < iterations;  i++)
                {
                    var value = "A random piece of data.";
                    var key = Guid.NewGuid();
                    keys.Add(key);
                    db.Save(key, Encoding.UTF8.GetBytes(value));
                }
                //var stats = DoStats(db);
                //Assert.AreEqual(100, stats.RecordCount);
            }
            
            watch.Stop();
            Console.Out.WriteLine(watch.ElapsedMilliseconds + "ms, " + iterations +" row");
            Console.Out.WriteLine("reading");
            watch.Reset();
            watch.Start();
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {   
                    var value = Encoding.UTF8.GetString(db.Get(key));
                    //Console.Out.WriteLine(key + ", "  + value);
                }
                //DoStats(db);
            }
            watch.Stop();
            Console.Out.WriteLine(watch.ElapsedMilliseconds + "ms, " + iterations + " row");

            Console.Out.WriteLine("updating to short value");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    db.Save(key, Encoding.UTF8.GetBytes("A value."));
                }
                DoStats(db);
            }
            Console.Out.WriteLine("reading");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    var value = Encoding.UTF8.GetString(db.Get(key));
                    //Console.Out.WriteLine(key + ", " + value);
                }
                DoStats(db);
            }

            Console.Out.WriteLine("updating to long value");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    db.Save(key, Encoding.UTF8.GetBytes("A much longer value than ever before will be updated."));
                }
                DoStats(db);
            }
            Console.Out.WriteLine("reading");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    var value = Encoding.UTF8.GetString(db.Get(key));
                    //Console.Out.WriteLine(key + ", " + value);
                }
                DoStats(db);
            }

            Console.Out.WriteLine("deleting...");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    db.Delete(key);
                }
                DoStats(db);
            }

            Console.Out.WriteLine("testing delete");
            using (var db = new GuidStore(indexDirectory))
            {
                foreach (var key in keys)
                {
                    try
                    {
                        db.Get(key);
                        Console.Out.WriteLine("hmmm... not good.");
                    }
                    catch(Exception ex)
                    {
                        //Console.Out.WriteLine("deleted.");        
                    }
                }
                DoStats(db);
                Console.Out.WriteLine("adding one more");
                db.Save(Guid.NewGuid(), Encoding.UTF8.GetBytes("test"));
                DoStats(db);

                Console.Out.WriteLine("compact");
                db.Compact();
                DoStats(db);
            }

        }

        private static CompactStatistics DoStats(GuidStore db)
        {
            var stats = db.GetCompactStatistics();
            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "current", stats.TotalSize, stats.IndexSize, stats.BlobSize);
            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "compact", stats.EstimatedTotalPostCompactSize, stats.EstimatedPostCompactIndexSize, stats.PostCompactBlobSize);
            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "records", stats.RecordCount,
                stats.RecordCount > 0 ?
                stats.IndexSize / stats.RecordCount : 0, 
                stats.RecordCount > 0 ? stats.EstimatedPostCompactIndexSize/ stats.RecordCount : 0);

            return stats;
        }

        [Test]
        public void TestDocumentStorage()
        {
        }

        [Test]
        public void TestIndex()
        {
            string utf8Test = @"
                いろはにほへど　ちりぬるを
                わがよたれぞ　つねならむ
                うゐのおくやま　けふこえて
                あさきゆめみじ　ゑひもせず (4)
                ";

            var guid = Guid.NewGuid();

            using (var tixS = new MemoryStream())
            using (var tdtS = new MemoryStream())
            using (var dixS = new MemoryStream())
            using (var ddtS = new MemoryStream())
            {
                using (var writer = new IndexWriter(tixS, tdtS, dixS, ddtS))
                {
                    var document = new bsharptree.example.simpleindex.Document(guid, new MemoryStream(Encoding.UTF8.GetBytes(utf8Test)));
                    writer.AddDocument(document, new DefaultAnalyzer());
                    writer.TermStorage.Index.Commit();
                }

                //tixS.Flush();
                //tdtS.Flush();
                //dixS.Flush();
                //ddtS.Flush();


                // reset
                tixS.Seek(0, SeekOrigin.Begin);
                tdtS.Seek(0, SeekOrigin.Begin);
                dixS.Seek(0, SeekOrigin.Begin);
                ddtS.Seek(0, SeekOrigin.Begin);

                using (var termStorage = new TermStorage(tixS, tdtS))
                {
                    var analyzer = new DefaultAnalyzer();
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(utf8Test));

                    foreach (var tp in analyzer.GetTermPositions(stream))
                    {
                        var positions = termStorage.Get(tp.Term);
                        var found = false;
                        foreach (var pos in positions.Value)
                        {
                            Assert.IsTrue(pos.Document.Equals(guid));
                            if (pos.Span.Start == tp.Span.Start && pos.Span.End == tp.Span.End) 
                                found = true;
                        }
                        Assert.IsTrue(found);
                    }
                }

                //var foo = new IndexQueryProvider();
                //var docStorage = new DocumentStorage(dixS, ddtS);
                //var results = from term in foo.Terms
                //              from location in term.Value
                //              where term.Key == "foo" || term.Key == "bar"
                //              select location;

                //foreach (var result in results.Distinct())
                //{
                //    Console.Out.WriteLine(result.Document);

                //    var document = docStorage.Get(result.Document);
                //    var context = docStorage.GetContext(result);
                //    Console.Out.WriteLine(context);
                //    Console.Out.WriteLine();
                //}

            }
        }

        [Test]
        public void TestUTF8()
        {
            // generate a test set of every valid set of bytes
            var utf8TestData = new MemoryStream();
            var _utf8Test = Encoding.UTF8.GetString( Properties.Resources.UTF_8_test);
            var data = Encoding.UTF8.GetBytes(_utf8Test);
            utf8TestData.Write(data, 0, data.Length);

            ////var buffer = new char[1];
            //for (int i = 0; i < 0x10FFFF; i++)
            //{
            //    if(i >= 0xD800 && i <= 0xDFFF) continue;

            //    //buffer[0] = Char.ConvertFromUtf32(i)[0];// (char)i;
            //    var bytes = Encoding.UTF8.GetBytes(Char.ConvertFromUtf32(i));
            //    utf8TestData.Write(bytes, 0, bytes.Length);
            //}
            utf8TestData.Flush();
            utf8TestData.Seek(0, SeekOrigin.Begin);

            var utf8 = new Utf8CharScanner(utf8TestData);

            //for (int i = 0; i < 0x10FFFF; i++)
            //{
            //    if (i >= 0xD800 && i <= 0xDFFF) continue;
            //    var expected = Char.ConvertFromUtf32(i);
            //    var actual = utf8.Read().Value;
            //    if(actual.HasValue)
            //        Assert.AreEqual(expected, actual.ToString(), string.Format("Failed at index {0}, exp {1}, act {2}", i, (int)i, (int)actual));
            //}
            Console.Out.WriteLine("utf len" + Encoding.UTF8.GetMaxByteCount(1));
            Console.Out.WriteLine(_utf8Test.Length);
            long offset=0;
            for (int i = 0; i < _utf8Test.Length; i++)
            {

                var expected = new CharLocation
                    {
                        Value = _utf8Test[i]
                    };

                var expectedBytes = Encoding.UTF8.GetBytes(new []{expected.Value.Value});
                if(expectedBytes.Length > 2)
                {
                    utf8.Read();
                    continue;
                }

                expected.ByteSpan.Start = offset;
                expected.ByteSpan.End = offset + expectedBytes.Length;
                offset += expectedBytes.Length;

                var actual = utf8.Read();

                Assert.AreEqual(expected.Value, actual.Value, "index: " + i);
                Assert.AreEqual(expected.ByteSpan.Start, actual.ByteSpan.Start, "index: " + i);
                string window = _utf8Test.Substring(i > 30? i - 30 : 0, 60);
                Assert.AreEqual(expected.ByteSpan.End, actual.ByteSpan.End, string.Format("index: {0}, expbyte: {1},  value {2}", i, expectedBytes.Length, window));
            }
            Console.Out.WriteLine(_utf8Test);
        }

//        private static string _utf8Test =
//            @"
//        いろはにほへど　ちりぬるを
//        わがよたれぞ　つねならむ
//        うゐのおくやま　けふこえて
//        あさきゆめみじ　ゑひもせず (4)
//        ";


//        [Test]
//        public void TestSimpleDB()
//        {
//            string indexDirectory = @"C:\test-simple-db\1\";
//            var keys = new List<Guid>();
//            int iterations = 10000;
//            Console.Out.WriteLine("building");
//            var watch = new Stopwatch();
//            watch.Start();
//            using (var db = new GuidStore(indexDirectory))
//            {
//                for (int i = 0; i < iterations; i++)
//                {
//                    var value = "A random piece of data.";
//                    var key = Guid.NewGuid();
//                    keys.Add(key);
//                    db.Save(key, Encoding.UTF8.GetBytes(value));
//                }
//                //var stats = DoStats(db);
//                //Assert.AreEqual(100, stats.RecordCount);
//            }

//            watch.Stop();
//            Console.Out.WriteLine(watch.ElapsedMilliseconds + "ms, " + iterations + " row");
//            Console.Out.WriteLine("reading");
//            watch.Reset();
//            watch.Start();
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    var value = Encoding.UTF8.GetString(db.Get(key));
//                    //Console.Out.WriteLine(key + ", "  + value);
//                }
//                //DoStats(db);
//            }
//            watch.Stop();
//            Console.Out.WriteLine(watch.ElapsedMilliseconds + "ms, " + iterations + " row");

//            Console.Out.WriteLine("updating to short value");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    db.Save(key, Encoding.UTF8.GetBytes("A value."));
//                }
//                DoStats(db);
//            }
//            Console.Out.WriteLine("reading");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    var value = Encoding.UTF8.GetString(db.Get(key));
//                    //Console.Out.WriteLine(key + ", " + value);
//                }
//                DoStats(db);
//            }

//            Console.Out.WriteLine("updating to long value");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    db.Save(key, Encoding.UTF8.GetBytes("A much longer value than ever before will be updated."));
//                }
//                DoStats(db);
//            }
//            Console.Out.WriteLine("reading");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    var value = Encoding.UTF8.GetString(db.Get(key));
//                    //Console.Out.WriteLine(key + ", " + value);
//                }
//                DoStats(db);
//            }

//            Console.Out.WriteLine("deleting...");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    db.Delete(key);
//                }
//                DoStats(db);
//            }

//            Console.Out.WriteLine("testing delete");
//            using (var db = new GuidStore(indexDirectory))
//            {
//                foreach (var key in keys)
//                {
//                    try
//                    {
//                        db.Get(key);
//                        Console.Out.WriteLine("hmmm... not good.");
//                    }
//                    catch (Exception ex)
//                    {
//                        //Console.Out.WriteLine("deleted.");        
//                    }
//                }
//                DoStats(db);
//                Console.Out.WriteLine("adding one more");
//                db.Save(Guid.NewGuid(), Encoding.UTF8.GetBytes("test"));
//                DoStats(db);

//                Console.Out.WriteLine("compact");
//                db.Compact();
//                DoStats(db);
//            }

//        }

//        private static CompactStatistics DoStats(GuidStore db)
//        {
//            var stats = db.GetCompactStatistics();
//            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "current", stats.TotalSize, stats.IndexSize, stats.BlobSize);
//            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "compact", stats.EstimatedTotalPostCompactSize, stats.EstimatedPostCompactIndexSize, stats.PostCompactBlobSize);
//            Console.Out.WriteLine("{0}: {1} [{2}, {3}]", "records", stats.RecordCount,
//                                  stats.RecordCount > 0 ?
//                                                            stats.IndexSize / stats.RecordCount : 0,
//                                  stats.RecordCount > 0 ? stats.EstimatedPostCompactIndexSize / stats.RecordCount : 0);

//            return stats;
//        }

//        [Test]
//        public void TestDocumentStorage()
//        {
//        }

//        [Test]
//        public void TestIndex()
//        {
//            string utf8Test = @"
//                いろはにほへど　ちりぬるを
//                わがよたれぞ　つねならむ
//                うゐのおくやま　けふこえて
//                あさきゆめみじ　ゑひもせず (4)
//                ";

//            var guid = Guid.NewGuid();

//            using (var tixS = new MemoryStream())
//            using (var tdtS = new MemoryStream())
//            using (var dixS = new MemoryStream())
//            using (var ddtS = new MemoryStream())
//            {
//                using (var writer = new IndexWriter(tixS, tdtS, dixS, ddtS))
//                {
//                    var document = new mockindex.Document(guid, new MemoryStream(Encoding.UTF8.GetBytes(utf8Test)));
//                    writer.AddDocument(document, new DefaultAnalyzer());
//                    writer.TermStorage.Index.Commit();
//                }

//                //tixS.Flush();
//                //tdtS.Flush();
//                //dixS.Flush();
//                //ddtS.Flush();


//                // reset
//                tixS.Seek(0, SeekOrigin.Begin);
//                tdtS.Seek(0, SeekOrigin.Begin);
//                dixS.Seek(0, SeekOrigin.Begin);
//                ddtS.Seek(0, SeekOrigin.Begin);

//                using (var termStorage = new TermStorage(tixS, tdtS))
//                {
//                    var analyzer = new DefaultAnalyzer();
//                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(utf8Test));

//                    foreach (var tp in analyzer.GetTermPositions(stream))
//                    {
//                        var positions = termStorage.Get(tp.Term);
//                        var found = false;
//                        foreach (var pos in positions.Value)
//                        {
//                            Assert.IsTrue(pos.Document.Equals(guid));
//                            if (pos.Span.Start == tp.Span.Start && pos.Span.End == tp.Span.End)
//                                found = true;
//                        }
//                        Assert.IsTrue(found);
//                    }
//                }

//                var foo = new IndexQueryProvider();
//                var docStorage = new DocumentStorage(dixS, ddtS);
//                var results = from term in foo.Terms
//                              from location in term.Value
//                              where term.Key == "foo" || term.Key == "bar"
//                              select location;

//                foreach (var result in results.Distinct())
//                {
//                    Console.Out.WriteLine(result.Document);

//                    var document = docStorage.Get(result.Document);
//                    var context = docStorage.GetContext(result);
//                    Console.Out.WriteLine(context);
//                    Console.Out.WriteLine();
//                }

//            }
//        }

//        [Test]
//        public void TestUTF8()
//        {
//            // generate a test set of every valid set of bytes
//            var utf8TestData = new MemoryStream();
//            var _utf8Test = Encoding.UTF8.GetString(Properties.Resources.UTF_8_test);
//            var data = Encoding.UTF8.GetBytes(_utf8Test);
//            utf8TestData.Write(data, 0, data.Length);

//            ////var buffer = new char[1];
//            //for (int i = 0; i < 0x10FFFF; i++)
//            //{
//            //    if(i >= 0xD800 && i <= 0xDFFF) continue;

//            //    //buffer[0] = Char.ConvertFromUtf32(i)[0];// (char)i;
//            //    var bytes = Encoding.UTF8.GetBytes(Char.ConvertFromUtf32(i));
//            //    utf8TestData.Write(bytes, 0, bytes.Length);
//            //}
//            utf8TestData.Flush();
//            utf8TestData.Seek(0, SeekOrigin.Begin);

//            var utf8 = new Utf8CharScanner(utf8TestData);

//            //for (int i = 0; i < 0x10FFFF; i++)
//            //{
//            //    if (i >= 0xD800 && i <= 0xDFFF) continue;
//            //    var expected = Char.ConvertFromUtf32(i);
//            //    var actual = utf8.Read().Value;
//            //    if(actual.HasValue)
//            //        Assert.AreEqual(expected, actual.ToString(), string.Format("Failed at index {0}, exp {1}, act {2}", i, (int)i, (int)actual));
//            //}
//            Console.Out.WriteLine("utf len" + Encoding.UTF8.GetMaxByteCount(1));
//            Console.Out.WriteLine(_utf8Test.Length);
//            long offset = 0;
//            for (int i = 0; i < _utf8Test.Length; i++)
//            {

//                var expected = new CharLocation
//                {
//                    Value = _utf8Test[i]
//                };

//                var expectedBytes = Encoding.UTF8.GetBytes(new[] { expected.Value.Value });
//                if (expectedBytes.Length > 2)
//                {
//                    utf8.Read();
//                    continue;
//                }

//                expected.ByteSpan.Start = offset;
//                expected.ByteSpan.End = offset + expectedBytes.Length;
//                offset += expectedBytes.Length;

//                var actual = utf8.Read();

//                Assert.AreEqual(expected.Value, actual.Value, "index: " + i);
//                Assert.AreEqual(expected.ByteSpan.Start, actual.ByteSpan.Start, "index: " + i);
//                string window = _utf8Test.Substring(i > 30 ? i - 30 : 0, 60);
//                Assert.AreEqual(expected.ByteSpan.End, actual.ByteSpan.End, string.Format("index: {0}, expbyte: {1},  value {2}", i, expectedBytes.Length, window));
//            }
//            Console.Out.WriteLine(_utf8Test);
//        }

//        //        private static string _utf8Test =
//        //            @"
//        //        いろはにほへど　ちりぬるを
//        //        わがよたれぞ　つねならむ
//        //        うゐのおくやま　けふこえて
//        //        あさきゆめみじ　ゑひもせず (4)
//        //        ";
        [Test]
        public void TestMockLinq()
        {
            var index = new SimpleIndex();
            index.AddItem(new mockindex.Document { Id = 1, Value=  "foo one" });
            index.AddItem(new mockindex.Document { Id = 2, Value = "two" });
            index.AddItem(new mockindex.Document { Id = 3, Value = "three two" });
            index.AddItem(new mockindex.Document { Id = 4, Value = "four six" });
            index.AddItem(new mockindex.Document { Id = 5, Value = "five two three" });
            index.AddItem(new mockindex.Document { Id = 6, Value = "five" });
            index.AddItem(new mockindex.Document { Id = 7, Value = "five two" });
            index.AddItem(new mockindex.Document { Id = 8, Value = "five three" });
            index.AddItem(new mockindex.Document { Id = 9, Value = "three" });

            foreach (var result in index.Inversions.Documents(DocumentComparer.Default))
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

            foreach (var term in index.Inversions)
            {
                //Console.Out.WriteLine("term: {0}, docs: {1}", term.Text, string.Join(",", term.Documents.Select(a=>a.DocID.ToString()).ToArray()));
            }
            //.ButNot("three")

            Console.Out.WriteLine("");
            
            Console.Out.WriteLine("==== five OR two OR three ");

            var results = index.QueryExecutor.MustHave("five").Should("two").Should("three");
            foreach (var result in results.Invertables())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

            Console.Out.WriteLine("==== five OR two OR three ");
            var subQuery = index.QueryExecutor.MustHave("two").MustNot("three");
            results = index.QueryExecutor.MustHave("five").Should(subQuery);

            foreach (var result in results.Invertables())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

        }

        [Test]
        public void TestBuildQuery()
        {
            var index = new SimpleIndex();
            index.AddItem(new mockindex.Document { Id = 1, Value = "foo one" });
            index.AddItem(new mockindex.Document { Id = 2, Value = "two" });
            index.AddItem(new mockindex.Document { Id = 3, Value = "three two" });
            index.AddItem(new mockindex.Document { Id = 4, Value = "four six" });
            index.AddItem(new mockindex.Document { Id = 5, Value = "five two three" });
            index.AddItem(new mockindex.Document { Id = 6, Value = "five" });
            index.AddItem(new mockindex.Document { Id = 7, Value = "five two" });
            index.AddItem(new mockindex.Document { Id = 8, Value = "five three" });
            index.AddItem(new mockindex.Document { Id = 9, Value = "three" });
            index.AddItem(new mockindex.Document { Id = 10, Value = "four two three" });



            foreach (var result in index.Inversions.Documents(DocumentComparer.Default))
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

            Console.Out.WriteLine("==== five OR (two -three)");
            var rootQuery = index.QueryExecutor.Should("five");
            var subQuery = index.QueryExecutor.Should("two").MustNot("three");

            var finalQuery = rootQuery.Should(subQuery);

            foreach (var result in finalQuery.Invertables())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

            //var queryText = "-six foo four +one";
            var queryText = "+(five (four AND two)) +(three two nothing here to see) -two";

            DoQuery(index, queryText, new StringInverter());

            //Console.Out.WriteLine("=== graph for " + queryText + "===");
            //var parser = new Parser(new Scanner());
            //var tree = parser.Parse(queryText);

            //var rootExpressionNode =
            //    tree.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(
            //        a => a.Token.Type == TokenType.Expression);
            
            //if(default(ParseNode) == rootExpressionNode)
            //    throw new Exception("No query in parse tree.");

            //var rootQueryClause = AnalyzeQueryNode(rootExpressionNode, );

            //var queryExecutor = index.GetQueryExecutor(rootQueryClause);

            //Console.Out.WriteLine("=== results for: " + queryText + " ===");
            //foreach (var result in queryExecutor.Invertables())
            //{
            //    Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            //}

            //foreach (var leafnode in FindLeafNodes(tree))
            //{
            //    // build leaf node
            //    // TERM|QUOTEDTERM OP? (TERM|QUOTEDTERM)?
            //    for (int i = 0; i < leafnode.Nodes.Count; i+=2)
            //    {
            //        var termExp = leafnode.Nodes[i];
            //        var termNode = termExp.Nodes.Find(a => a.Token.Type == TokenType.TERM || a.Token.Type == TokenType.QUOTEDTERM);
            //        if (null == termNode) throw new Exception("WTF");
            //        var term = termNode.Token.ToString();

            //        var prefixNode = termExp.Nodes.Find(a => a.Token.Type == TokenType.PREFIX);
            //        if (null != prefixNode)
            //        {
                        
            //        }
            //    }


            //    PrintParseNode(leafnode);
            //}

        }

        [Test]
        public void TestSomeExamples()
        {
            IIndex<int, string, string> index = new SimpleIndex();
            IInverter<string, string> inverter = new StringInverter();

            index.AddItem(new mockindex.Document { Id = 1, Value = "A B C D E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 2, Value = "B C" }, inverter);
            index.AddItem(new mockindex.Document { Id = 3, Value = "A B E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 4, Value = "C D E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 5, Value = "E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 6, Value = "D D D D E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 7, Value = "D E F" }, inverter);
            //            index.AddDocument(new Document { DocID = 8, Text = "A B C D E F" }, inverter);
            //            index.AddDocument(new Document { DocID = 9, Text = "A B C D E F" }, inverter);
            index.AddItem(new mockindex.Document { Id = 10, Value = "A B" }, inverter);
            index.AddItem(new mockindex.Document { Id = 11, Value = "E E E A B" }, inverter);
            index.AddItem(new mockindex.Document { Id = 12, Value = "C A B" }, inverter);

            foreach (var result in index.Inversions.Documents(DocumentComparer.Default))
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }

            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match clause A and clause B (other clauses don't affect matching) ");
            Console.Out.WriteLine("-------------------------");
            var queryText = "A AND B OR C OR D OR E OR F";
            DoQuery(index, queryText, inverter);
            
            queryText = "+A +B C D E F";
            DoQuery(index, queryText, inverter);

            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents matching at least one of these clauses ");
            Console.Out.WriteLine("-------------------------");
            queryText = "C OR D OR E OR F";
            DoQuery(index, queryText, inverter);

            queryText = "C D E F";
            DoQuery(index, queryText, inverter);
            
            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match A, and match one of B, C, D, E, or F ");
            Console.Out.WriteLine("-------------------------");
            queryText = "A AND (B OR C OR D OR E OR F)";
            DoQuery(index, queryText, inverter);

            queryText = "+A +(B C D E F)";
            DoQuery(index, queryText, inverter);
            
            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match at least one of C, D, E, F, or both of A and B");
            Console.Out.WriteLine("-------------------------");
            queryText = "(A AND B) OR C OR D OR E OR F ";
            DoQuery(index, queryText, inverter);

            queryText = "(+A +B) C D E F";
            DoQuery(index, queryText, inverter);
        }

        private static void DoQuery(IIndexReader<int, string, string> index, string queryText, IInverter<string, string> inverter)
        {
            Console.Out.WriteLine("=== graph for " + queryText + "===");

            var results = index.ExecuteQuery(queryText, inverter);

            Console.Out.WriteLine("=== results for: " + queryText + " ===");

            foreach (var result in results)
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
            }
        }

        //    var parser = new Parser(new Scanner());
        //    var tree = parser.Parse(queryText);

        //    var rootExpressionNode =
        //        tree.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(
        //            a => a.Token.Type == TokenType.Expression);

        //    if (default(ParseNode) == rootExpressionNode)
        //        throw new Exception("No query in parse tree.");

        //    var rootQueryClause = AnalyzeQueryNode(rootExpressionNode, inverter);

        //    var queryExecutor = index.GetQueryExecutor(rootQueryClause);

        //    Console.Out.WriteLine("=== results for: " + queryText + " ===");
        //    foreach (var result in queryExecutor.Invertables())
        //    {
        //        Console.Out.WriteLine("doc: {0}, text: {1}", result.Id, result.Value);
        //    }
        //}

        //private static QueryClause<string> AnalyzeQueryNode(ParseNode node, IInverter<string, string> inverter, QueryClauseFlag flag = QueryClauseFlag.Should)
        //{
        //    // assume we are working with an expression node.
        //    if(node.Token.Type != TokenType.Expression)
        //        throw new Exception("Must be an expression node!");

        //    var queryClause = new QueryClause { Flag = flag };
        //    var precedingOperator = "OR";

        //    for (int i = 0; i < node.Nodes.Count; i++)
        //    {
        //        var subnode = node.Nodes[i];

        //        switch (subnode.Token.Type)
        //        {
        //            case TokenType.MustClause:
        //            case TokenType.MustNotClause:
        //            case TokenType.Clause:

        //                var followingOperator = GetFollowingOperator(node, i);
        //                var queryClauseFlag = GetQueryClauseFlag(subnode, precedingOperator, followingOperator);
                        
        //                // determine if the clause is a term or subclause
        //                foreach (var childnode in subnode.Nodes)
        //                {
        //                    switch (childnode.Token.Type)
        //                    {
        //                        case TokenType.SubClause:
        //                            var expressionNode =
        //                                childnode.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Expression);

        //                            if (expressionNode != default(ParseNode))
        //                            {
        //                                var subClause = AnalyzeQueryNode(expressionNode, inverter, queryClauseFlag);
        //                                queryClause.AddSubClause(subClause);
        //                            }
        //                            break;
        //                        case TokenType.Term:
        //                            var termNode =
        //                                childnode.Nodes.FirstOrDefault(
        //                                    a => a.Token.Type == TokenType.TERM || a.Token.Type == TokenType.QUOTEDTERM);

        //                            if (termNode != default(ParseNode))
        //                            {
        //                                var units = inverter.Invert(termNode.Token.Text);
        //                                foreach(var unit in units)
        //                                    queryClause.AddUnit(unit, queryClauseFlag);
        //                            }

        //                            break;
        //                    }
        //                }
        //                break;
        //            case TokenType.OPERATOR:
        //                precedingOperator = subnode.Token.Text;
        //                break;
        //        }
        //    }
        //    return queryClause;
        //}

        //private static string GetFollowingOperator(ParseNode node, int i)
        //{
        //    if ((i + 1) < node.Nodes.Count)
        //    {
        //        var followingNode = node.Nodes[i + 1];
        //        if (followingNode.Token.Type == TokenType.OPERATOR)
        //            return followingNode.Token.Text;
        //    }

        //    return "OR"; // default
        //}

        //private static QueryClauseFlag GetQueryClauseFlag(ParseNode subnode, string precedingOperator, string followingOperator)
        //{
        //    // note: +/- modifiers have higher precedence than conjunction operators, 
        //    // and the conjunction operator closest to the clause wins. AND OR AND NOT foo == NOT foo
        //    switch (subnode.Token.Type)
        //    {
        //        case TokenType.MustClause:
        //            return QueryClauseFlag.Must;
        //        case TokenType.MustNotClause:
        //            return QueryClauseFlag.MustNot;
        //        case TokenType.Clause:
        //            if(precedingOperator == "NOT")
        //                return QueryClauseFlag.MustNot;

        //            // note: this causes preceding operator to have higher precedence than following
        //            if(precedingOperator == "AND" || followingOperator == "AND") 
        //                return QueryClauseFlag.Must;

        //            break;
        //    }
        //    return QueryClauseFlag.Should;
        //}
    }


}
