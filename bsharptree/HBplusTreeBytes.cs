using System;
using System.IO;
using System.Text;
using bsharptree.definition;
using bsharptree.io;
using bsharptree.toolkit;

namespace bsharptree
{
    /// <summary>
    /// Btree mapping unlimited length key strings to fixed length hash values
    /// </summary>
    public class HBplusTreeBytes<TKey> : XBplusTreeBytes<TKey>
        where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        public HBplusTreeBytes(BplusTreeBytes<TKey> tree, int hashLength)
            : base(tree, hashLength)
        {
            // null out the culture context to use the naive comparison
            Tree.NoCulture();
        }

        public new static HBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, nodesize, buffersize, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, cultureId, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> Initialize(string treefileName, string blockfileName, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefileName, blockfileName, prefixLength, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, int nodesize, int buffersize, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, nodesize, buffersize, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, cultureId, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> Initialize(Stream treefile, Stream blockfile, int prefixLength, IConverter<TKey, byte[]> keyConverter)
        {
            return new HBplusTreeBytes<TKey>(
                BplusTreeBytes<TKey>.Initialize(treefile, blockfile, prefixLength, keyConverter),
                prefixLength);
        }

        public new static HBplusTreeBytes<TKey> ReOpen(Stream treefile, Stream blockfile, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReOpen(treefile, blockfile, keyConverter);
            var prefixLength = tree.MaxKeyLength();
            return new HBplusTreeBytes<TKey>(tree, prefixLength);
        }

        public new static HBplusTreeBytes<TKey> ReOpen(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReOpen(treefileName, blockfileName, keyConverter);
            var prefixLength = tree.MaxKeyLength();
            return new HBplusTreeBytes<TKey>(tree, prefixLength);
        }

        public new static HBplusTreeBytes<TKey> ReadOnly(string treefileName, string blockfileName, IConverter<TKey, byte[]> keyConverter)
        {
            var tree = BplusTreeBytes<TKey>.ReadOnly(treefileName, blockfileName, keyConverter);
            var prefixLength = tree.MaxKeyLength();
            return new HBplusTreeBytes<TKey>(tree, prefixLength);
        }

        //public override TKey PrefixForByteCount(TKey s, int maxbytecount)
        //{
        //    var inputbytes = StringTools.StringToBytes(s);
        //    var d = MD5.Create();
        //    var digest = d.ComputeHash(inputbytes);
        //    var resultbytes = new byte[maxbytecount];

        //    // copy digest translating to printable ascii
        //    for (var i = 0; i < maxbytecount; i++)
        //    {
        //        int r = digest[i%digest.Length];
        //        if (r > 127)
        //        {
        //            r = 256 - r;
        //        }
        //        if (r < 0)
        //        {
        //            r = -r;
        //        }
        //        //Console.WriteLine(" before "+i+" "+r);
        //        r = r%79 + 40; // printable ascii
        //        //Console.WriteLine(" "+i+" "+r);
        //        resultbytes[i] = (byte) r;
        //    }
            
        //    var result = StringTools.BytesToString(resultbytes);

        //    return result;
        //}

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Tree.ToString());
            sb.Append("\r\n* key / hash / value dump *\r\n");

            var currentkey = FirstKey();
            while (currentkey != null)
            {
                sb.Append("\r\n" + currentkey);
                sb.Append(" / " + StringTools.PrintableString(PrefixForByteCount(currentkey, PrefixLength).ToString()));
                try
                {
                    sb.Append(" / value found ");
                }
                catch (Exception)
                {
                    sb.Append(" !!!!!!! FAILED TO GET VALUE");
                }
                currentkey = NextKey(currentkey);
            }

            return sb.ToString();
        }
    }
}