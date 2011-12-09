using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using bsharptree.example.simpledb;
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

                var foo = new IndexQueryProvider();
                var docStorage = new DocumentStorage(dixS, ddtS);
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

        [Test]
        public void TestMockLinq()
        {
            var index = new SimpleIndex();
            index.AddDocument(new Document { DocID = 1, Text=  "foo one" });
            index.AddDocument(new Document { DocID = 2, Text = "two" });
            index.AddDocument(new Document { DocID = 3, Text = "three two" });
            index.AddDocument(new Document { DocID = 4, Text = "four six" });
            index.AddDocument(new Document { DocID = 5, Text = "five two three" });
            index.AddDocument(new Document { DocID = 6, Text = "five" });
            index.AddDocument(new Document { DocID = 7, Text = "five two" });
            index.AddDocument(new Document { DocID = 8, Text = "five three" });
            index.AddDocument(new Document { DocID = 9, Text = "three" });

            foreach (var result in index.Terms.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

            foreach (var term in index.Terms)
            {
                //Console.Out.WriteLine("term: {0}, docs: {1}", term.Text, string.Join(",", term.Documents.Select(a=>a.DocID.ToString()).ToArray()));
            }
            //.ButNot("three")

            Console.Out.WriteLine("");
            
            Console.Out.WriteLine("==== five OR two OR three ");

            var results = index.QueryExecutor.MustHave("five").Should("two").Should("three");
            foreach (var result in results.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

            Console.Out.WriteLine("==== five OR two OR three ");
            var subQuery = index.QueryExecutor.MustHave("two").MustNot("three");
            results = index.QueryExecutor.MustHave("five").Should(subQuery);

            foreach (var result in results.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

        }

        //[Test]
        //public void TestParser()
        //{
        //    var index = new SimpleIndex();
        //    index.AddDocument(new Document { DocID = 1, Text = "foo one" });
        //    index.AddDocument(new Document { DocID = 2, Text = "two" });
        //    index.AddDocument(new Document { DocID = 3, Text = "three two" });
        //    index.AddDocument(new Document { DocID = 4, Text = "four six" });
        //    index.AddDocument(new Document { DocID = 5, Text = "five two three" });
        //    index.AddDocument(new Document { DocID = 6, Text = "five" });
        //    index.AddDocument(new Document { DocID = 7, Text = "five two" });
        //    index.AddDocument(new Document { DocID = 8, Text = "five three" });
        //    index.AddDocument(new Document { DocID = 9, Text = "three" });
        //    index.AddDocument(new Document { DocID = 10, Text = "two three" });

        //    foreach (var result in index.Terms.Documents())
        //    {
        //        Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
        //    }

        //    foreach (var term in index.Terms)
        //    {
        //        //Console.Out.WriteLine("term: {0}, docs: {1}", term.Text, string.Join(",", term.Documents.Select(a=>a.DocID.ToString()).ToArray()));
        //    }
        //    //.ButNot("three")

        //    Console.Out.WriteLine("");

        //    Console.Out.WriteLine("==== five OR two OR three ");

        //    var results = index.QueryExecutor.MustHave("five").Should("two").Should("three");
        //    foreach (var result in results.Documents())
        //    {
        //        Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
        //    }

        //    Console.Out.WriteLine("==== five OR (two -three)");
        //    var subQuery = index.QueryExecutor.MustHave("two").MustNot("three");
        //    results = index.QueryExecutor.MustHave("five").Should(subQuery);

        //    foreach (var result in results.Documents())
        //    {
        //        Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
        //    }

        //    var queryText = "five OR (two -three)";

        //    var parsedQuery = index.QueryExecutor;

        //    var parser = new Parser(new Scanner());
        //    var tree = parser.Parse(queryText);
        //    var nodes = new Queue<ParseNode>();
        //    nodes.Enqueue(tree.Parent);

        //    while (nodes.Count > 0)
        //    {
        //        var node = nodes.Dequeue();

        //        var token = node.Token;

        //        switch (token.Type)
        //        {
        //            case TokenType.Node:

        //                var term = node.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.TERM).Token.Value.ToString();

        //                if(node.Nodes[0].Token.Type == TokenType.PREFIX)
        //                {
        //                    if(node.Nodes[0].Token.Value.ToString() == "-")
        //                    {
        //                        parsedQuery = parsedQuery.MustNot(term);
        //                    }
        //                    else if(node.Nodes[0].Token.Value.ToString() == "+")
        //                    {
        //                        parsedQuery = parsedQuery.MustHave(term);
        //                    }
        //                }
        //                else
        //                {
        //                    parsedQuery = parsedQuery.Should(term);
        //                }

        //                break;
        //            case TokenType.TermExp:
                        
        //                break;
        //            default:
        //                throw new ArgumentOutOfRangeException();
        //        }

        //        foreach (var childNode in node.Nodes)
        //            nodes.Enqueue(childNode);
        //    }
        //}

        //[Test]
        //public void TestFindDeepestNodes()
        //{
        //    var queryText = "foo (bar AND (baz OR bob)) AND (buzz) AND (j AND (i OR (k or m)))";
        //    var parser = new Parser(new Scanner());
        //    var tree = parser.Parse(queryText);
        //    var deepestNodes = FindDeepestNodes(tree);
        //    foreach (var deepestNode in deepestNodes)
        //    {
        //        PrintParseNode(deepestNode);
        //    }
        //}

        //[Test]
        //public void TestFindLeafNodes()
        //{
        //    var queryText = "foo (bar AND (baz OR bob)) AND (buzz) AND (j AND (i OR (k OR m)))";
        //    var parser = new Parser(new Scanner());
        //    var tree = parser.Parse(queryText);
        //    foreach (var leafnode in FindLeafNodes(tree))
        //    {
        //        PrintParseNode(leafnode);
        //    }
        //}

        [Test]
        public void TestBuildQuery()
        {
            var index = new SimpleIndex();
            index.AddDocument(new Document { DocID = 1, Text = "foo one" });
            index.AddDocument(new Document { DocID = 2, Text = "two" });
            index.AddDocument(new Document { DocID = 3, Text = "three two" });
            index.AddDocument(new Document { DocID = 4, Text = "four six" });
            index.AddDocument(new Document { DocID = 5, Text = "five two three" });
            index.AddDocument(new Document { DocID = 6, Text = "five" });
            index.AddDocument(new Document { DocID = 7, Text = "five two" });
            index.AddDocument(new Document { DocID = 8, Text = "five three" });
            index.AddDocument(new Document { DocID = 9, Text = "three" });
            index.AddDocument(new Document { DocID = 10, Text = "four two three" });
            


            foreach (var result in index.Terms.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

            Console.Out.WriteLine("==== five OR (two -three)");
            var rootQuery = index.QueryExecutor.Should("five");
            var subQuery = index.QueryExecutor.Should("two").MustNot("three");

            var finalQuery = rootQuery.Should(subQuery);

            foreach (var result in finalQuery.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

            //var queryText = "-six foo four +one";
            var queryText = "+(five (four AND two)) +(three two nothing here to see) -two";

            Console.Out.WriteLine("=== graph for " + queryText + "===");
            var parser = new Parser(new Scanner());
            var tree = parser.Parse(queryText);

            var rootExpressionNode =
                tree.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(
                    a => a.Token.Type == TokenType.Expression);
            
            if(default(ParseNode) == rootExpressionNode)
                throw new Exception("No query in parse tree.");

            var rootQueryClause = AnalyzeQueryNode(rootExpressionNode);

            QueryExecutor queryExecutor = index.GetQueryExecutor(rootQueryClause);

            Console.Out.WriteLine("=== results for: " + queryText + " ===");
            foreach (var result in queryExecutor.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

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
            var index = new SimpleIndex();
            index.AddDocument(new Document { DocID = 1, Text = "A B C D E F" });
            index.AddDocument(new Document { DocID = 2, Text = "B C" });
            index.AddDocument(new Document { DocID = 3, Text = "A B E F" });
            index.AddDocument(new Document { DocID = 4, Text = "C D E F" });
            index.AddDocument(new Document { DocID = 5, Text = "E F" });
            index.AddDocument(new Document { DocID = 6, Text = "D D D D E F" });
            index.AddDocument(new Document { DocID = 7, Text = "D E F" });
//            index.AddDocument(new Document { DocID = 8, Text = "A B C D E F" });
//            index.AddDocument(new Document { DocID = 9, Text = "A B C D E F" });
            index.AddDocument(new Document { DocID = 10, Text = "A B" });
            index.AddDocument(new Document { DocID = 11, Text = "E E E A B" });
            index.AddDocument(new Document { DocID = 12, Text = "C A B" });
            
            foreach (var result in index.Terms.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }

            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match clause A and clause B (other clauses don't affect matching) ");
            Console.Out.WriteLine("-------------------------");
            var queryText = "A AND B OR C OR D OR E OR F";
            DoQuery(index, queryText);
            
            queryText = "+A +B C D E F";
            DoQuery(index, queryText);

            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents matching at least one of these clauses ");
            Console.Out.WriteLine("-------------------------");
            queryText = "C OR D OR E OR F";
            DoQuery(index, queryText);

            queryText = "C D E F";
            DoQuery(index, queryText);
            
            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match A, and match one of B, C, D, E, or F ");
            Console.Out.WriteLine("-------------------------");
            queryText = "A AND (B OR C OR D OR E OR F)";
            DoQuery(index, queryText);

            queryText = "+A +(B C D E F)";
            DoQuery(index, queryText);
            
            Console.Out.WriteLine("-------------------------");
            Console.Out.WriteLine("find documents that match at least one of C, D, E, F, or both of A and B");
            Console.Out.WriteLine("-------------------------");
            queryText = "(A AND B) OR C OR D OR E OR F ";
            DoQuery(index, queryText);

            queryText = "(+A +B) C D E F";
            DoQuery(index, queryText);
        }

        private static void DoQuery(SimpleIndex index, string queryText)
        {
            Console.Out.WriteLine("=== graph for " + queryText + "===");
            var parser = new Parser(new Scanner());
            var tree = parser.Parse(queryText);

            var rootExpressionNode =
                tree.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(
                    a => a.Token.Type == TokenType.Expression);

            if (default(ParseNode) == rootExpressionNode)
                throw new Exception("No query in parse tree.");

            var rootQueryClause = AnalyzeQueryNode(rootExpressionNode);

            QueryExecutor queryExecutor = index.GetQueryExecutor(rootQueryClause);

            Console.Out.WriteLine("=== results for: " + queryText + " ===");
            foreach (var result in queryExecutor.Documents())
            {
                Console.Out.WriteLine("doc: {0}, text: {1}", result.DocID, result.Text);
            }
        }



        //private QueryExecutor UpdateQuery(QueryExecutor rootQueryExecutor, SimpleIndex index, ParseNode node)
        //{
        //    QueryExecutor subQueryExecutor = null;
        //    // TERM|QUOTEDTERM OP? (TERM|QUOTEDTERM)?

        //    for (int i = 0; i < node.Nodes.Count; i++)
        //    {
        //        var termExp = node.Nodes[i];
        //        Console.Out.WriteLine("t->" + termExp.Token.Type);

        //        if (termExp.Token.Type == TokenType.GroupExp)
        //        {
        //            subQueryExecutor = UpdateQuery(subQueryExecutor, index, termExp.Nodes.Find(a => a.Token.Type == TokenType.Node));
        //            continue;
        //        }

        //        if (termExp.Token.Type != TokenType.TermExp) continue;
            

        //        var termNode = termExp.Nodes.Find(a => a.Token.Type == TokenType.Term).
        //            Nodes.
        //            Find(a => a.Token.Type == TokenType.TERM || a.Token.Type == TokenType.QUOTEDTERM);
                
        //        if (null == termNode) throw new Exception("WTF");
        //        Console.Out.WriteLine("tn->" + termNode.Token.Type);

        //        var prefixNode = termExp.Nodes.Find(a => a.Token.Type == TokenType.PREFIX);

        //        var term = termNode.Token.Text;

        //        // first one..
        //        if (i == 0)
        //        {
        //            if (null != prefixNode && prefixNode.Token.Text== "-")
        //                subQueryExecutor = index.QueryExecutor.MustNot(term);
        //            else
        //                subQueryExecutor = index.QueryExecutor.MustHave(term);
        //        }
        //        else
        //        {
        //            var previousToken = node.Nodes[i - 1].Token;
        //            if(previousToken.Type == TokenType.OPERATOR && previousToken.Text == "AND")
        //            {
        //                subQueryExecutor = subQueryExecutor.MustHave(term);
        //            }
        //            subQueryExecutor.Should(term);

        //            if (null != prefixNode)
        //            {
        //                if (prefixNode.Token.Text == "-")
        //                {
        //                    subQueryExecutor = subQueryExecutor.MustNot(term);
        //                }
        //                else
        //                {
        //                    subQueryExecutor = subQueryExecutor.MustHave(term);
        //                }
        //            }
        //        }
        //    }

        //    if (rootQueryExecutor == null)
        //    {
        //        Console.Out.WriteLine("!$");
        //        return subQueryExecutor;
        //    }

        //    // determine combination operator for node...
        //    var parent = node.Parent;
        //    var grandparent = parent.Parent;
        //    if (grandparent != null)
        //    {
        //        var nodeIndex = grandparent.Nodes.IndexOf(parent);
        //        if (nodeIndex > 0)
        //        {
        //            var maybeOperator = grandparent.Nodes[nodeIndex - 1];
        //            if (maybeOperator.Token.Type == TokenType.OPERATOR && maybeOperator.Token.Text == "AND")
        //            {
        //                return rootQueryExecutor.MustHave(subQueryExecutor);
        //            }
        //        }
        //    } 

        //    return rootQueryExecutor.Should(subQueryExecutor);
        //}

        private static QueryClause AnalyzeQueryNode(ParseNode node, QueryClauseFlag flag = QueryClauseFlag.Should)
        {
            // assume we are working with an expression node.
            if(node.Token.Type != TokenType.Expression)
                throw new Exception("Must be an expression node!");

            var queryClause = new QueryClause { Flag = flag };
            var precedingOperator = "OR";

            for (int i = 0; i < node.Nodes.Count; i++)
            {
                var subnode = node.Nodes[i];

                switch (subnode.Token.Type)
                {
                    case TokenType.MustClause:
                    case TokenType.MustNotClause:
                    case TokenType.Clause:

                        var followingOperator = GetFollowingOperator(node, i);
                        var queryClauseFlag = GetQueryClauseFlag(subnode, precedingOperator, followingOperator);
                        
                        // determine if the clause is a term or subclause
                        foreach (var childnode in subnode.Nodes)
                        {
                            switch (childnode.Token.Type)
                            {
                                case TokenType.SubClause:
                                    var expressionNode =
                                        childnode.Nodes.FirstOrDefault(a => a.Token.Type == TokenType.Expression);

                                    if (expressionNode != default(ParseNode))
                                    {
                                        var subClause = AnalyzeQueryNode(expressionNode, queryClauseFlag);
                                        queryClause.AddSubClause(subClause);
                                    }
                                    break;
                                case TokenType.Term:
                                    var termNode =
                                        childnode.Nodes.FirstOrDefault(
                                            a => a.Token.Type == TokenType.TERM || a.Token.Type == TokenType.QUOTEDTERM);

                                    if (termNode != default(ParseNode)) queryClause.AddTerm(termNode.Token.Text, queryClauseFlag);

                                    break;
                            }
                        }
                        break;
                    case TokenType.OPERATOR:
                        precedingOperator = subnode.Token.Text;
                        break;
                }
            }
            return queryClause;
        }

        private static string GetFollowingOperator(ParseNode node, int i)
        {
            if ((i + 1) < node.Nodes.Count)
            {
                var followingNode = node.Nodes[i + 1];
                if (followingNode.Token.Type == TokenType.OPERATOR)
                    return followingNode.Token.Text;
            }

            return "OR"; // default
        }

        private static QueryClauseFlag GetQueryClauseFlag(ParseNode subnode, string precedingOperator, string followingOperator)
        {
            // note: +/- modifiers have higher precedence than conjunction operators, 
            // and the conjunction operator closest to the clause wins. AND OR AND NOT foo == NOT foo
            switch (subnode.Token.Type)
            {
                case TokenType.MustClause:
                    return QueryClauseFlag.Must;
                case TokenType.MustNotClause:
                    return QueryClauseFlag.MustNot;
                case TokenType.Clause:
                    if(precedingOperator == "NOT")
                        return QueryClauseFlag.MustNot;

                    // note: this causes preceding operator to have higher precedence than following
                    if(precedingOperator == "AND" || followingOperator == "AND") 
                        return QueryClauseFlag.Must;

                    break;
            }
            return QueryClauseFlag.Should;
        }

        private static void PrintParseNode(ParseNode parseNode)
        {
            var nodes = new Queue<NodeWithDepth>();
            nodes.Enqueue(new NodeWithDepth { Depth = 0, Node = parseNode });
            
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();

                Console.Out.WriteLine(string.Empty.PadLeft(node.Depth, ' ') + node.Node.Text);

                foreach (var childNode in node.Node.Nodes)
                    nodes.Enqueue(new NodeWithDepth { Depth = node.Depth + 1, Node = childNode });
            }
        }

        //private static List<ParseNode> FindDeepestNodes(ParseTree tree)
        //{
        //    var nodes = new Queue<NodeWithDepth>();
        //    nodes.Enqueue(new NodeWithDepth { Depth = 0, Node = tree });

        //    int maxDepth = 0;
        //    var nodesAtMaxDepth = new List<NodeWithDepth>();

        //    while (nodes.Count > 0)
        //    {
        //        var node = nodes.Dequeue();

        //        var token = node.Node.Token;

        //        if (token.Type == TokenType.Node)
        //        {
        //            if(nodesAtMaxDepth.Count == 0 || nodesAtMaxDepth.Max(a=>a.Depth) < node.Depth)
        //            {
        //                nodesAtMaxDepth.Clear();
        //                maxDepth = node.Depth;
        //            }

        //            if(maxDepth == node.Depth)
        //                nodesAtMaxDepth.Add(node);
        //        }
        //        foreach (var childNode in node.Node.Nodes)
        //            nodes.Enqueue(new NodeWithDepth { Depth = node.Depth + 1, Node = childNode });
        //    }

        //    return nodesAtMaxDepth.Select(a => a.Node).ToList();
        //}

        //private static IEnumerable<ParseNode> FindLeafNodes(ParseTree tree)
        //{
        //    var nodes = new Queue<ParseNode>();
        //    nodes.Enqueue(tree.Nodes.FirstOrDefault(a=>a.Token.Type == TokenType.Start).Nodes.FirstOrDefault(a=>a.Token.Type == TokenType.Node));

        //    while (nodes.Count > 0)
        //    {
        //        var node = nodes.Dequeue();

        //        var token = node.Token;
                
        //        var isLeafNode = true;
        //        foreach (var childGroupExpNode in node.Nodes.Where(a=>a.Token.Type == TokenType.GroupExp))
        //        {
        //            isLeafNode = false;
        //            foreach (var childNode in childGroupExpNode.Nodes.Where(a => a.Token.Type == TokenType.Node))
        //                nodes.Enqueue(childNode);
        //        }

        //        if (isLeafNode) 
        //            yield return node;
        //    }
        //}

        private struct NodeWithDepth
        {
            public ParseNode Node;
            public int Depth;
        }
    }

    public class QueryClause
    {
        public QueryClauseFlag Flag { get; set; }

        public List<string> MustTerms = new List<string>();
        public List<string> MustNotTerms = new List<string>();
        public List<string> ShouldTerms = new List<string>();

        public List<QueryClause> MustSubClauses = new List<QueryClause>();
        public List<QueryClause> MustNotSubClauses = new List<QueryClause>();
        public List<QueryClause> ShouldSubClauses = new List<QueryClause>();

        public void AddTerm(string term, QueryClauseFlag flag)
        {
            switch (flag)
            {
                case QueryClauseFlag.Should:
                    if(!ShouldTerms.Contains(term))
                        ShouldTerms.Add(term);
                    break;
                case QueryClauseFlag.Must:
                    if (!MustTerms.Contains(term))
                        MustTerms.Add(term);
                    break;
                case QueryClauseFlag.MustNot:
                    if (!MustNotTerms.Contains(term))
                        MustNotTerms.Add(term);
                    break;
            }
        }
        public void AddSubClause(QueryClause subClause)
        {
            switch (subClause.Flag)
            {
                case QueryClauseFlag.Should:
                    if (!ShouldSubClauses.Contains(subClause))
                        ShouldSubClauses.Add(subClause);
                    break;
                case QueryClauseFlag.Must:
                    if (!MustSubClauses.Contains(subClause))
                        MustSubClauses.Add(subClause);
                    break;
                case QueryClauseFlag.MustNot:
                    if (!MustNotSubClauses.Contains(subClause))
                        MustNotSubClauses.Add(subClause);
                    break;
            }
        }
    }

    public enum QueryClauseFlag
    {
        Should = 0,
        Must = 1,
        MustNot = 2
    }

    public class SimpleIndex
    {
        public IEnumerable<Term> Terms 
        { 
            get
            {
                return _terms.Values;
            }
        }
        private Dictionary<string, Term> _terms = new Dictionary<string, Term>();
        public void AddDocument(Document doc)
        {
            var words = doc.Text.Split(' ');
            foreach (var word in words)
            {
                Term term;
                if (!_terms.TryGetValue(word, out term))
                {
                    term = new Term { Text = word };
                    _terms.Add(word, term);
                }
                if (!term.Documents.Contains(doc, DocumentComparer.Default)) 
                    term.Documents.Add(doc);
            }
        }
        public QueryExecutor QueryExecutor { get { return new QueryExecutor(Terms); } }

        public QueryExecutor GetQueryExecutor(QueryClause clause)
        {
            return GetQueryExecutor(QueryExecutor, clause);
        }

        public QueryExecutor GetQueryExecutor(QueryExecutor state, QueryClause clause)
        {
            var clauseExecutor = state;

            if (clause.MustTerms.Count == 0 && clause.MustSubClauses.Count == 0)
            {
                ///var shouldState = new QueryExecutor(Terms, new List<Document>());
                clauseExecutor = new QueryExecutor(Terms, new List<Document>());



                foreach (var element in clause.ShouldTerms)
                    clauseExecutor = clauseExecutor.Should(element);

                foreach (var element in clause.ShouldSubClauses)
                    clauseExecutor = clauseExecutor.Should(GetQueryExecutor(clauseExecutor, element));


                //state = AggregateQueryOperations(clause.ShouldTerms, state, state.Should);

                //state = clause.ShouldTerms.Aggregate(state, (current, term) => current.Should(term));
                //state = clause.ShouldSubClauses.Aggregate(state, (current, subclause) => current.Should(GetQueryExecutor(current, subclause)));
            }
            else
            {
                clauseExecutor = new QueryExecutor(Terms, Terms.Documents());


                foreach (var element in clause.MustTerms)
                    clauseExecutor = clauseExecutor.MustHave(element);

                foreach (var element in clause.MustSubClauses)
                    clauseExecutor = clauseExecutor.MustHave(GetQueryExecutor(clauseExecutor, element));

                //state = clause.MustTerms.Aggregate(state, (current, term) => current.MustHave(term));
                //state = clause.MustSubClauses.Aggregate(state, (current, subclause) => current.MustHave(GetQueryExecutor(current, subclause)));
            }

            foreach (var element in clause.MustNotTerms)
                clauseExecutor = clauseExecutor.MustNot(element);

            foreach (var element in clause.MustNotSubClauses)
                clauseExecutor = clauseExecutor.MustNot(GetQueryExecutor(clauseExecutor, element));

            //state = clause.MustNotTerms.Aggregate(state, (current, term) => current.MustNot(term));
            //state = clause.MustNotSubClauses.Aggregate(state, (current, subclause) => current.MustNot(GetQueryExecutor(current, subclause)));

            switch (clause.Flag)
            {
                case QueryClauseFlag.Should:
                    state = state.Should(clauseExecutor);
                    break;
                case QueryClauseFlag.Must:
                    state = state.MustHave(clauseExecutor);
                    break;
                case QueryClauseFlag.MustNot:
                    state = state.MustNot(clauseExecutor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return state;

        }

        // hmmm...
        public QueryExecutor AggregateQueryOperations<T>(IEnumerable<T> queryElements, QueryExecutor state, Func<T, QueryExecutor> queryOperation)
        {
            return queryElements.Aggregate(state, (current, term) => queryOperation(term));
        }
    }

    public class QueryExecutor
    {
        public QueryExecutor(IEnumerable<Term> allTerms, IEnumerable<Document> docs)
        {
            _allTerms = allTerms;
            _results = docs;
        }

        public QueryExecutor(IEnumerable<Term> allTerms)
            : this(allTerms, new List<Document>())// allTerms.Documents())
        {
        }

        private readonly IEnumerable<Term> _allTerms;

        private IEnumerable<Document> _results;
        public IEnumerable<Document> Documents()
        {
            return _results;
        }

        public QueryExecutor Should(string term)
        {
            _results = _results.Should(_allTerms, term);
            return this;
        }
        public QueryExecutor MustNot(string term)
        {
            _results = _results.MustNot(_allTerms, term);
            return this;
        }
        public QueryExecutor MustHave(string term)
        {
            _results = _results.MustHave(_allTerms, term);
            return this;
        }

        public QueryExecutor Should(QueryExecutor clause)
        {
            Console.Out.WriteLine("should have clause");
            _results = _results.Should(clause.Documents());
            return this;
        }
        public QueryExecutor MustNot(QueryExecutor clause)
        {
            Console.Out.WriteLine("must not have clause");
            _results = _results.MustNot(clause.Documents());
            return this;
        }
        public QueryExecutor MustHave(QueryExecutor clause)
        {
            Console.Out.WriteLine("must have clause");
            _results = _results.MustHave(clause.Documents());
            return this;
        }
    }

    public class Document
    {
        public Document()
        {
            DocID = 0;
            Text = string.Empty;
        }
        public int DocID;

        public string Text;
    }

    public class Term
    {
        public Term()
        {
            Text = string.Empty;
            Documents = new List<Document>();
        }
        public string Text;
        
        public List<Document> Documents;
    }

    public class TermComparer : IEqualityComparer<Term>
    {
        public static TermComparer Default = new TermComparer();
        public bool Equals(Term x, Term y)
        {
            if (x == null && y == null) return true;
            if (x == null || y==null) return false;

            return x.Text.Equals(y.Text);
        }

        public int GetHashCode(Term obj)
        {
            return obj == null ? default(int) : obj.Text.GetHashCode();
        }
    }

    public class DocumentComparer : IEqualityComparer<Document>
    {
        public static DocumentComparer Default = new DocumentComparer();
        public bool Equals(Document x, Document y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return x.DocID.Equals(y.DocID);
        }

        public int GetHashCode(Document obj)
        {
            return obj == null ? default(int) : obj.DocID.GetHashCode();
        }
    }

    public static class QueryExtensions
    {
        public static IEnumerable<Document> Documents(this IEnumerable<Term> results)
        {
            Console.Out.WriteLine("docs t, ");
            return results.SelectMany(a => a.Documents).Distinct(DocumentComparer.Default);
        }
        public static IEnumerable<Document> Documents(this IEnumerable<Term> terms, string term)
        {
            Console.Out.WriteLine("docs " + term + ", ");
            return terms.Where(a => a.Text == term).Documents();
        }
        public static IEnumerable<Document> Should(this IEnumerable<Document> results, IEnumerable<Term> allTerms, string term)
        {
            Console.Out.WriteLine("should have " + term + ", ");
            return results.Should(allTerms.Documents(term));
        }
        public static IEnumerable<Document> MustNot(this IEnumerable<Document> results, IEnumerable<Term> allTerms, string term)
        {
            Console.Out.WriteLine("must not have " + term + ", ");
            return results.MustNot(allTerms.Documents(term));
        }
        public static IEnumerable<Document> MustHave(this IEnumerable<Document> results, IEnumerable<Term> allTerms, string term)
        {
            Console.Out.WriteLine("must have " + term + ", ");
            return results.MustHave(allTerms.Documents(term));
        }
        public static IEnumerable<Document> Should(this IEnumerable<Document> results, IEnumerable<Document> other)
        {
            //return results.Concat(other).Distinct(DocumentComparer.Default);
            return results.Union(other, DocumentComparer.Default);
        }
        public static IEnumerable<Document> MustNot(this IEnumerable<Document> results, IEnumerable<Document> other)
        {
            return results.Where(a=> !other.Contains(a, DocumentComparer.Default));
        }
        public static IEnumerable<Document> MustHave(this IEnumerable<Document> results, IEnumerable<Document> other)
        {
            //return results.Join(other, a => a.DocID, b => b.DocID, (a, b) => a);
            return results.Where(other.Contains);//.Union(other.Where(results.Contains));
        }
    }
}
