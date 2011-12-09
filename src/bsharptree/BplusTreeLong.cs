using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using bsharptree.definition;
using bsharptree.exception;
using bsharptree.io;
using bsharptree.toolkit;

// delete next

namespace bsharptree
{
    /// <summary>
    /// Bplustree mapping fixed length strings (byte sequences) to longs (seek positions in file indexed).
    /// "Next leaf pointer" is not used since it increases the chance of file corruption on failure.
    /// All modifications are "shadowed" until a flush of all modifications succeeds.  Modifications are
    /// "hardened" when the header record is rewritten with a new root.  This design trades a few "unneeded"
    /// buffer writes for lower likelihood of file corruption.
    /// </summary>
    public class BplusTreeLong<TKey> : ITreeIndex<TKey, long> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        public const byte Version = 0;
        public static byte[] Headerprefix = {98, 112, 78, 98, 112};

        // size of allocated key space in each node (should be a read only property)
        public const int Nullbuffernumber = -1;
        public static byte Nonleaf, Leaf = 1, Free = 2;

        public HashSet<long> FreeBuffersOnAbort { get; set; }
        public HashSet<long> FreeBuffersOnCommit { get; set; }

        public bool DontUseCulture { get; set; }
        public int KeyLength { get; private set; }
        public int NodeSize { get; private set; }
        public BufferFile Buffers { get; private set; }

        // should be read only
        public int Buffersize { get; private set; }

        public CultureInfo CultureContext { get; set; }
        public Stream Fromfile { get; set; }
        public long SeekStart { get; set; }

        private readonly Dictionary<int, BplusNode<TKey>> _idToTerminalNode = new Dictionary<int, BplusNode<TKey>>();
        private readonly Dictionary<BplusNode<TKey>, int> _terminalNodeToId = new Dictionary<BplusNode<TKey>, int>();

        public readonly int Headersize = Headerprefix.Length + 1 + ByteTools.IntStorage * 3 + ByteTools.LongStorage * 2;
        private int _fifoLimit = 100;
        private int _lowerTerminalNodeCount;
        private int _terminalNodeCount;
        
        private long _freeHeadSeek;
        protected BplusNode<TKey> Root;
        protected long RootSeek;


        public BplusTreeLong(Stream fromfile, int keyLength, int nodeSize, long startSeek, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            KeyConverter = keyConverter;
            ValueConverter = 
                new GenericConverter<long, byte[]>(a => 
                    ByteTools.RetrieveLong(a,0), //BitConverter.ToInt64(a, 0), 
                    //BitConverter.GetBytes
                    a=>
                        {
                            var longBuffer = new byte[ByteTools.LongStorage];
                            ByteTools.Store(a, longBuffer, 0);
                            return longBuffer;
                        }
                    );
            
            FreeBuffersOnAbort = new HashSet<long>();
            FreeBuffersOnCommit = new HashSet<long>();

            CultureContext = new CultureInfo(cultureId);
            
            Fromfile = fromfile;
            NodeSize = nodeSize;
            SeekStart = startSeek;
            // add in key prefix overhead
            KeyLength = keyLength + ByteTools.ShortStorage;
            RootSeek = Nullbuffernumber;
            Root = null;
            _freeHeadSeek = Nullbuffernumber;
            SanityCheck();
        }

        public BplusTreeLong(Stream fromfile, int keyLength, int nodeSize, int cultureId, IConverter<TKey, byte[]> keyConverter) :
            this(fromfile, keyLength, nodeSize, 0, cultureId, keyConverter)
        {
            // just start seek at 0
        }

        public long this[TKey key]
        {
            get
            {
                long valueFound;
                bool test = ContainsKey(key, out valueFound);
                if (!test)
                {
                    throw new BplusTreeKeyMissingException("no such key found: " + key);
                }
                return valueFound;
            }
            set
            {
                if (!BplusNode<TKey>.IsKeyValid(key, this))
                    throw new BplusTreeBadKeyValueException("null or too large key cannot be inserted into tree: " + key);

                bool rootinit = false;
                if (Root == null)
                {
                    // allocate root
                    Root = new BplusNode<TKey>(this, null, -1, true);
                    rootinit = true;
                    //this.rootSeek = root.DumpToFreshBuffer();
                }
                // insert into root...
                TKey splitString;
                BplusNode<TKey> splitNode;
                Root.Insert(key, value, out splitString, out splitNode);
                if (splitNode != null)
                {
                    // split of root: make a new root.
                    rootinit = true;
                    BplusNode<TKey> oldRoot = Root;
                    Root = BplusNode<TKey>.BinaryRoot(oldRoot, splitString, splitNode, this);
                }
                if (rootinit)
                {
                    RootSeek = Root.DumpToFreshBuffer();
                }
                // check size in memory
                ShrinkFootprint();
            }
        }

        public IConverter<TKey, byte[]> KeyConverter { get; set; }
        public IConverter<long, byte[]> ValueConverter { get; set; }

        #region ITreeIndex Members

        public void Shutdown()
        {
            Fromfile.Flush();
            Fromfile.Close();
        }

        //public int Compare(TKey left, TKey right)
        //{
        //    if (left, default(TKey)))
        //        if (right, default(TKey)))
        //            return 0;
        //        else
        //            return -1;

        //    if (right, default(TKey)))
        //            return 1;
            
        //    return left.CompareTo(right);

        //    ////System.Globalization.CompareInfo cmp = this.cultureContext.CompareInfo;
        //    //if (CultureContext == null || DontUseCulture)
        //    //{
        //    //    // no culture context: use miscellaneous total ordering on unicode strings
        //    //    int i = 0;
        //    //    while (i < left.Length && i < right.Length)
        //    //    {
        //    //        int leftOrd = Convert.ToInt32(left[i]);
        //    //        int rightOrd = Convert.ToInt32(right[i]);
        //    //        if (leftOrd < rightOrd)
        //    //        {
        //    //            return -1;
        //    //        }
        //    //        if (leftOrd > rightOrd)
        //    //        {
        //    //            return 1;
        //    //        }
        //    //        i++;
        //    //    }
        //    //    if (left.Length < right.Length)
        //    //    {
        //    //        return -1;
        //    //    }
        //    //    if (left.Length > right.Length)
        //    //    {
        //    //        return 1;
        //    //    }
        //    //    return 0;
        //    //}
        //    //if (_cmp == null)
        //    //{
        //    //    _cmp = CultureContext.CompareInfo;
        //    //}
        //    //return _cmp.Compare(left, right);
        //}

        public void Recover(bool correctErrors)
        {
            var visited = new Hashtable();

            // find all reachable nodes
            if (Root != null)
                Root.SanityCheck(visited);

            // traverse the free list
            var freebuffernumber = _freeHeadSeek;
            while (freebuffernumber != Nullbuffernumber)
            {
                if (visited.ContainsKey(freebuffernumber))
                    throw new BplusTreeException("free buffer visited twice " + freebuffernumber);

                visited[freebuffernumber] = Free;
                freebuffernumber = ParseFreeBuffer(freebuffernumber);
            }

            // find out what is missing
            var missing = new HashSet<long>();
            var maxbuffer = Buffers.NextBufferNumber();

            for (long i = 0; i < maxbuffer; i++)
            {
                if (!visited.ContainsKey(i))
                    missing.Add(i);
            }

            // remove from missing any free-on-commit blocks
            foreach (var tobefreed in FreeBuffersOnCommit)
                missing.Remove(tobefreed);

            // add the missing values to the free list
            if (correctErrors)
            {
                if (missing.Count > 0)
                    Debug.WriteLine("correcting " + missing.Count + " unreachable buffers");

                var missingL = missing.ToList();

                missingL.Sort();
                missingL.Reverse();

                foreach (var buffernumber in missingL)
                    DeallocateBuffer(buffernumber);

                //this.ResetBookkeeping();
            }
            else if (missing.Count > 0)
            {
                var buffers = 
                    missing.Cast<DictionaryEntry>()
                    .Aggregate(string.Empty, (current, thing) => current + (" " + thing.Key));

                throw new BplusTreeException("found " + missing.Count + " unreachable buffers." + buffers);
            }
        }

        public void SetFootPrintLimit(int limit)
        {
            if (limit < 5)
                throw new BplusTreeException("foot print limit less than 5 is too small");

            _fifoLimit = limit;
        }

        public void RemoveKey(TKey key)
        {
            if (Root == null)
            {
                throw new BplusTreeKeyMissingException("tree is empty: cannot delete");
            }
            bool mergeMe;
            BplusNode<TKey> theroot = Root;
            theroot.Delete(key, out mergeMe);
            // if the root is not a leaf and contains only one child (no key), reroot
            if (mergeMe && !Root.IsLeaf && Root.SizeInUse() == 0)
            {
                Root = Root.FirstChild;
                RootSeek = Root.MakeRoot();
                theroot.Free();
            }
        }

        public TKey FirstKey()
        {
            var result = default(TKey);

            if (Root == null)
                return result;

            // empty string is smallest possible tree
            if (!ContainsKey(default(TKey)))
                return Root.FindNextKey(default(TKey));

            ShrinkFootprint();

            return result;
        }

        public TKey NextKey(TKey afterThisKey)
        {
            if (afterThisKey == null)
                throw new BplusTreeBadKeyValueException("cannot search for null string");

            var result = Root.FindNextKey(afterThisKey);
            
            ShrinkFootprint();
            
            return result;
        }

        public bool ContainsKey(TKey key)
        {
            long valueFound;
            return ContainsKey(key, out valueFound);
        }

        public bool UpdateKey(TKey key, long value)
        {
            bool result = false;

            if (Root != null)
                result = Root.UpdateMatch(key, value);

            ShrinkFootprint();

            return result;
        }

        /// <summary>
        /// Store off any changed buffers, clear the fifo, free invalid buffers
        /// </summary>
        public void Commit()
        {
            // store all modifications
            if (Root != null)
                RootSeek = Root.Invalidate(false);

            Fromfile.Flush();

            // commit the new root
            SetHeader();
            Fromfile.Flush();
            
            // at this point the changes are committed, but some space is unreachable.
            // now free all unfreed buffers no longer in use
            var toFree = FreeBuffersOnCommit.ToList();

            toFree.Sort();
            toFree.Reverse();

            foreach (var buffernumber in toFree)
                DeallocateBuffer(buffernumber);

            // store the free list head
            SetHeader();
            Fromfile.Flush();
            ResetBookkeeping();
        }

        /// <summary>
        /// Forget all changes since last commit
        /// </summary>
        public void Abort()
        {
            // deallocate allocated blocks
            var toFree = FreeBuffersOnAbort.ToList();

            toFree.Sort();
            toFree.Reverse();

            foreach (var buffernumber in toFree)
                DeallocateBuffer(buffernumber);

            var freehead = _freeHeadSeek;

            // reread the header (except for freelist head)
            ReadHeader();

            // restore the root
            if (RootSeek == Nullbuffernumber)
            {
                Root = null; // nothing was committed
            }
            else
            {
                Root.LoadFromBuffer(RootSeek);
            }

            ResetBookkeeping();
            _freeHeadSeek = freehead;
            SetHeader(); // store new freelist head
            Fromfile.Flush();
        }

        #endregion

        public int MaxKeyLength()
        {
            return KeyLength - ByteTools.ShortStorage;
        }

        public void SanityCheck(bool strong)
        {
            SanityCheck();
            if (!strong) return;
            
            Recover(false);

            // look at all deferred deallocations -- they should not be free
            var buffer = new byte[1];

            foreach (var buffernumber  in FreeBuffersOnAbort)
            {
                Buffers.GetBuffer(buffernumber, buffer, 0, 1);

                if (buffer[0] == Free)
                    throw new BplusTreeException("free on abort buffer already marked free " + buffernumber);
            }

            foreach (var buffernumber in FreeBuffersOnCommit)
            {
                Buffers.GetBuffer(buffernumber, buffer, 0, 1);

                if (buffer[0] == Free)
                    throw new BplusTreeException("free on commit buffer already marked free " + buffernumber);
            }
        }

        //public void SerializationCheck()
        //{
        //    if (Root == null)
        //        throw new BplusTreeException("serialization check requires initialized root, sorry");

        //    Root.SerializationCheck();
        //}

        private void SanityCheck()
        {
            if (NodeSize < 2)
                throw new BplusTreeException("node size must be larger than 2");

            if (KeyLength < 5)
                throw new BplusTreeException("Key length must be larger than 5");

            if (SeekStart < 0)
                throw new BplusTreeException("start seek may not be negative");

            // compute the buffer size
            // indicator | seek position | [ key storage | seek position ]*
            var keystorage = KeyLength + ByteTools.ShortStorage;

            Buffersize = 1 + ByteTools.LongStorage + (keystorage + ByteTools.LongStorage)*NodeSize;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("== BplusTree ==");
            sb.AppendLine();
            
            sb.AppendLine("nodesize=" + NodeSize);
            sb.AppendLine("seekstart=" + SeekStart);
            sb.AppendLine("rootseek=" + RootSeek);
            sb.Append("free on commit " + FreeBuffersOnCommit.Count + " ::");
            
            foreach (var bufferNumber in FreeBuffersOnCommit)
            {
                sb.Append(" " + bufferNumber);
            }
            
            sb.AppendLine();
            sb.Append("Freebuffers : ");

            var freevisit = new HashSet<long>();
            long free = _freeHeadSeek;
            string allfree = "freehead=" + free + " :: ";

            while (free != Nullbuffernumber)
            {
                allfree = allfree + " " + free;
                
                if (freevisit.Contains(free))
                    throw new BplusTreeException("cycle in freelist " + free);

                freevisit.Add(free);
                free = ParseFreeBuffer(free);
            }

            sb.Append(allfree.Length == 0 ? "empty list" : allfree);

            foreach (var bufferNumber in FreeBuffersOnCommit)
            {
                sb.Append(" " + bufferNumber);
            }
            sb.AppendLine();

            sb.Append("free on abort " + FreeBuffersOnAbort.Count + " ::");
            foreach (var bufferNumber in FreeBuffersOnAbort)
            {
                sb.Append(" " + bufferNumber);
            }
            sb.AppendLine();

            //... add more
            if (Root == null)
            {
                sb.AppendLine("* NULL ROOT *");
            }
            else
            {
               sb.Append(Root.ToString());
            }

            return sb.ToString();
        }

        public static BplusTreeLong<TKey> SetupFromExistingStream(Stream fromfile, IConverter<TKey, byte[]> keyConverter)
        {
            return SetupFromExistingStream(fromfile, 0, keyConverter);
        }

        public static BplusTreeLong<TKey> SetupFromExistingStream(Stream fromfile, long startSeek, IConverter<TKey, byte[]> keyConverter)
        {
            var dummyId = CultureInfo.InvariantCulture.LCID;

            var tree = new BplusTreeLong<TKey>(fromfile, 100, 7, startSeek, dummyId, keyConverter); // dummy values for nodesize, keysize
            tree.ReadHeader();
            tree.Buffers = BufferFile.SetupFromExistingStream(fromfile, startSeek + tree.Headersize);
            
            if (tree.Buffers.Buffersize != tree.Buffersize)
                throw new BplusTreeException("inner and outer buffer sizes should match");

            if (tree.RootSeek != Nullbuffernumber)
            {
                tree.Root = new BplusNode<TKey>(tree, null, -1, true);
                tree.Root.LoadFromBuffer(tree.RootSeek);
            }

            return tree;
        }

        public static BplusTreeLong<TKey> InitializeInStream(Stream fromfile, int keyLength, int nodeSize, IConverter<TKey, byte[]> keyConverter)
        {
            var dummyId = CultureInfo.InvariantCulture.LCID;
            return InitializeInStream(fromfile, keyLength, nodeSize, dummyId, keyConverter);
        }

        public static BplusTreeLong<TKey> InitializeInStream(Stream fromfile, int keyLength, int nodeSize, int cultureId, IConverter<TKey, byte[]> keyConverter)
        {
            return InitializeInStream(fromfile, keyLength, nodeSize, cultureId, 0, keyConverter);
        }

        public static BplusTreeLong<TKey> InitializeInStream(Stream fromfile, int keyLength, int nodeSize, int cultureId, long startSeek, IConverter<TKey, byte[]> keyConverter)
        {
            if (fromfile.Length > startSeek)
                throw new BplusTreeException("can't initialize bplus tree inside written area of stream");

            var result = new BplusTreeLong<TKey>(fromfile, keyLength, nodeSize, startSeek, cultureId, keyConverter);
            result.SetHeader();
            result.Buffers = BufferFile.InitializeBufferFileInStream(fromfile, result.Buffersize, startSeek + result.Headersize);
            
            return result;
        }

        public bool ContainsKey(TKey key, out long valueFound)
        {
            //if (key, default(TKey)))

            //    throw new BplusTreeBadKeyValueException("cannot search for null string");

            bool result = false;
            valueFound = 0;

            if (Root != null)
                result = Root.FindMatch(key, out valueFound);

            ShrinkFootprint();

            return result;
        }

        private void ResetBookkeeping()
        {
            FreeBuffersOnCommit.Clear();
            FreeBuffersOnAbort.Clear();
            _idToTerminalNode.Clear();
            _terminalNodeToId.Clear();
        }

        public long AllocateBuffer()
        {
            long allocated;
            if (_freeHeadSeek == Nullbuffernumber)
            {
                // should be written immediately after allocation
                allocated = Buffers.NextBufferNumber();

                //System.Diagnostics.Debug.WriteLine("<br> allocating fresh buffer "+allocated);

                return allocated;
            }

            // get the free head data
            allocated = _freeHeadSeek;
            _freeHeadSeek = ParseFreeBuffer(allocated);

            //System.Diagnostics.Debug.WriteLine("<br> recycling free buffer "+allocated);

            return allocated;
        }

        private long ParseFreeBuffer(long buffernumber)
        {
            const int freesize = 1 + ByteTools.LongStorage;

            var buffer = new byte[freesize];
            Buffers.GetBuffer(buffernumber, buffer, 0, freesize);

            if (buffer[0] != Free)
                throw new BplusTreeException("free buffer not marked free");

            var result = ByteTools.RetrieveLong(buffer, 1);

            return result;
        }

        public void DeallocateBuffer(long buffernumber)
        {
            //System.Diagnostics.Debug.WriteLine("<br> deallocating "+buffernumber);
            const int freesize = 1 + ByteTools.LongStorage;
            var buffer = new byte[freesize];

            // it better not already be marked free
            Buffers.GetBuffer(buffernumber, buffer, 0, 1);
            if (buffer[0] == Free)
                throw new BplusTreeException("attempt to re-free free buffer not allowed");

            buffer[0] = Free;
            
            ByteTools.Store(_freeHeadSeek, buffer, 1);
            Buffers.SetBuffer(buffernumber, buffer, 0, freesize);
            
            _freeHeadSeek = buffernumber;
        }

        private void SetHeader()
        {
            byte[] header = MakeHeader();
            Fromfile.Seek(SeekStart, SeekOrigin.Begin);
            Fromfile.Write(header, 0, header.Length);
        }

        public void RecordTerminalNode(BplusNode<TKey> terminalNode)
        {
            if (terminalNode == Root)
                return; // never record the root node

            if (_terminalNodeToId.ContainsKey(terminalNode))
                return; // don't record it again

            var id = _terminalNodeCount;

            _terminalNodeCount++;
            _terminalNodeToId[terminalNode] = id;
            _idToTerminalNode[id] = terminalNode;
        }

        public void ForgetTerminalNode(BplusNode<TKey> nonterminalNode)
        {
            if (!_terminalNodeToId.ContainsKey(nonterminalNode))
                return; // silently ignore (?)

            var id = _terminalNodeToId[nonterminalNode];
            if (id == _lowerTerminalNodeCount)
                _lowerTerminalNodeCount++;

            _idToTerminalNode.Remove(id);
            _terminalNodeToId.Remove(nonterminalNode);
        }

        public void ShrinkFootprint()
        {
            InvalidateTerminalNodes(_fifoLimit);
        }

        public void InvalidateTerminalNodes(int toLimit)
        {
            while (_terminalNodeToId.Count > toLimit)
            {
                // choose oldest nonterminal and deallocate it
                while (!_idToTerminalNode.ContainsKey(_lowerTerminalNodeCount))
                {
                    _lowerTerminalNodeCount++; // since most nodes are terminal this should usually be a short walk

                    //System.Diagnostics.Debug.WriteLine("<BR>WALKING "+this.LowerTerminalNodeCount);
                    //System.Console.WriteLine("<BR>WALKING "+this.LowerTerminalNodeCount);

                    if (_lowerTerminalNodeCount > _terminalNodeCount)
                        throw new BplusTreeException("internal error counting nodes, lower limit went too large");
                }
                
                //System.Console.WriteLine("<br> done walking");
                var id = _lowerTerminalNodeCount;
                var victim = _idToTerminalNode[id];

                //System.Diagnostics.Debug.WriteLine("\r\n<br>selecting "+victim.myBufferNumber+" for deallocation from fifo");
                _idToTerminalNode.Remove(id);
                _terminalNodeToId.Remove(victim);

                if (victim.MyBufferNumber != Nullbuffernumber)
                    victim.Invalidate(true);
            }
        }

        private void ReadHeader()
        {
            // prefix | version | node size | key size | culture id | buffer number of root | buffer number of free list head
            var header = new byte[Headersize];
            
            Fromfile.Seek(SeekStart, SeekOrigin.Begin);
            Fromfile.Read(header, 0, Headersize);

            var index = 0;
            // check prefix
            foreach (byte b in Headerprefix)
            {
                if (header[index] != b)
                    throw new BufferFileException("invalid header prefix");

                index++;
            }

            // skip version (for now)
            index++;
            NodeSize = ByteTools.Retrieve(header, index);
            index += ByteTools.IntStorage;

            KeyLength = ByteTools.Retrieve(header, index);
            index += ByteTools.IntStorage;
            
            var cultureId = ByteTools.Retrieve(header, index);
            CultureContext = new CultureInfo(cultureId);
            index += ByteTools.IntStorage;

            RootSeek = ByteTools.RetrieveLong(header, index);
            index += ByteTools.LongStorage;

            _freeHeadSeek = ByteTools.RetrieveLong(header, index);
            
            SanityCheck();
            //this.header = header;
        }

        public byte[] MakeHeader()
        {
            // prefix | version | node size | key size | culture id | buffer number of root | buffer number of free list head
            var result = new byte[Headersize];
            Headerprefix.CopyTo(result, 0);
            result[Headerprefix.Length] = Version;
            
            int index = Headerprefix.Length + 1;
            ByteTools.Store(NodeSize, result, index);
            index += ByteTools.IntStorage;
            
            ByteTools.Store(KeyLength, result, index);
            index += ByteTools.IntStorage;
            
            ByteTools.Store(
                CultureContext != null 
                    ? CultureContext.LCID 
                    : CultureInfo.InvariantCulture.LCID, 
                result,
                index
                );
            index += ByteTools.IntStorage;
            
            ByteTools.Store(RootSeek, result, index);
            index += ByteTools.LongStorage;

            ByteTools.Store(_freeHeadSeek, result, index);
            
            return result;
        }
    }
}