using System;
using System.Collections;
using System.Text;
using bsharptree.exception;
using bsharptree.toolkit;

namespace bsharptree
{
    public class BplusNode<TKey> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        /// <summary>
        /// Create a new BplusNode and install in parent if parent is not null.
        /// </summary>
        /// <param name="owner">tree containing the node</param>
        /// <param name="parent">parent node (if provided)</param>
        /// <param name="indexInParent">location in parent if provided</param>
        /// <param name="isLeaf"></param>
        public BplusNode(BplusTreeLong<TKey> owner, BplusNode<TKey> parent, int indexInParent, bool isLeaf)
        {
            IsLeaf = isLeaf;
            _owner = owner;
            _parent = parent;
            _size = owner.NodeSize;
            //this.isValid = true;

            _dirty = true;
            
            //			this.ChildBufferNumbers = new long[this.Size+1];
            //			this.ChildKeys = new string[this.Size];
            //			this.MaterializedChildNodes = new BplusNode[this.Size+1];
            
            Clear();

            if (parent == null || indexInParent < 0)
            {
                MyBufferNumber = BplusTreeLong<TKey>.Nullbuffernumber;
                return;
            }

            if (indexInParent > _size)
                throw new BplusTreeException("parent index too large");

            // key info, etc, set elsewhere
            _parent.MaterializedChildNodes[indexInParent] = this;
            MyBufferNumber = _parent.ChildBufferNumbers[indexInParent];
            _indexInParent = indexInParent;
        }

        // number of children used by this node
        //int NumberOfValidKids = 0;
        protected long[] ChildBufferNumbers;
        protected TKey[] ChildKeys;
        protected BplusNode<TKey>[] MaterializedChildNodes;

        private bool _dirty = true;
        private int _size;
        private int _indexInParent = -1;
        private BplusTreeLong<TKey> _owner;
        private BplusNode<TKey> _parent;

        public bool IsLeaf { get; private set; }
        public long MyBufferNumber { get; private set; }
        //public IConverter<TValue, byte[]> ValueConverter { get; set; }
        //public IConverter<TKey, byte[]> KeyConverter { get; set; }

        public BplusNode<TKey> FirstChild
        {
            get
            {
                var result = MaterializeNodeAtIndex(0);

                if (result == null)
                    throw new BplusTreeException("no first child");

                return result;
            }
        }

        public long MakeRoot()
        {
            _parent = null;
            _indexInParent = -1;
            if (MyBufferNumber == BplusTreeLong<TKey>.Nullbuffernumber)
            {
                throw new BplusTreeException("no root seek allocated to new root");
            }
            return MyBufferNumber;
        }

        public void Free()
        {
            if (MyBufferNumber != BplusTreeLong<TKey>.Nullbuffernumber)
            {
                if (_owner.FreeBuffersOnAbort.Contains(MyBufferNumber))
                {
                    // free it now
                    _owner.FreeBuffersOnAbort.Remove(MyBufferNumber);
                    _owner.DeallocateBuffer(MyBufferNumber);
                }
                else
                {
                    // free on commit
                    //this.owner.FreeBuffersOnCommit.Add(this.myBufferNumber);
                    _owner.FreeBuffersOnCommit.Add(MyBufferNumber);
                }
            }
            MyBufferNumber = BplusTreeLong<TKey>.Nullbuffernumber; // don't do it twice...
        }

        //public void SerializationCheck()
        //{
        //    var bplusNode = new BplusNode<TKey>(_owner, null, -1, false);
        //    for (int i = 0; i < _size; i++)
        //    {
        //        long j = i*(0xf0f0f0f0f0f0f01);
        //        bplusNode.ChildBufferNumbers[i] = j;
        //        bplusNode.ChildKeys[i] = "k" + i;
        //    }

        //    bplusNode.ChildBufferNumbers[_size] = 7;
        //    bplusNode.TestRebuffer();
        //    bplusNode.IsLeaf = true;
        //    for (int i = 0; i < _size; i++)
        //    {
        //        long j = -i*(0x3e3e3e3e3e3e666);
        //        bplusNode.ChildBufferNumbers[i] = j;
        //        bplusNode.ChildKeys[i] = "key" + i;
        //    }
        //    bplusNode.ChildBufferNumbers[_size] = -9097;
        //    bplusNode.TestRebuffer();
        //}

        //private void TestRebuffer()
        //{
        //    var isLeaf = IsLeaf;
        //    long[] bufferNumbers = ChildBufferNumbers;
        //    TKey[] childKeys = ChildKeys;

        //    var buffer = new byte[_owner.Buffersize];
        //    Dump(buffer);
        //    Clear();
        //    Load(buffer);
        //    for (int i = 0; i < _size; i++)
        //    {
        //        if (ChildBufferNumbers[i] != bufferNumbers[i])
        //        {
        //            throw new BplusTreeException("didn't get back buffernumber " + i + " got " + ChildBufferNumbers[i] +
        //                                         " not " + bufferNumbers[i]);
        //        }
        //        if (!ChildKeys[i].Equals(childKeys[i]))
        //        {
        //            throw new BplusTreeException("didn't get back key " + i + " got " + ChildKeys[i] + " not " + childKeys[i]);
        //        }
        //    }
        //    if (ChildBufferNumbers[_size] != bufferNumbers[_size])
        //    {
        //        throw new BplusTreeException("didn't get back buffernumber " + _size + " got " + ChildBufferNumbers[_size] +
        //                                     " not " + bufferNumbers[_size]);
        //    }
        //    if (IsLeaf != isLeaf)
        //    {
        //        throw new BplusTreeException("isLeaf should be " + isLeaf + " got " + IsLeaf);
        //    }
        //}

        public TKey SanityCheck(Hashtable visited)
        {
            if (visited == null)
                visited = new Hashtable();

            if (visited.ContainsKey(this))
                throw new BplusTreeException("node visited twice " + MyBufferNumber);

            visited[this] = MyBufferNumber;

            if (MyBufferNumber != BplusTreeLong<TKey>.Nullbuffernumber)
            {
                if (visited.ContainsKey(MyBufferNumber))
                    throw new BplusTreeException("buffer number seen twice " + MyBufferNumber);

                visited[MyBufferNumber] = this;
            }
            if (_parent != null)
            {
                if (_parent.IsLeaf)
                    throw new BplusTreeException("parent is leaf");

                _parent.MaterializeNodeAtIndex(_indexInParent);

                if (_parent.MaterializedChildNodes[_indexInParent] != this)
                    throw new BplusTreeException("incorrect index in parent");

                // since not at root there should be at least size/2 keys
                int limit = _size/2;
                if (IsLeaf)
                    limit--;

                for (int i = 0; i < limit; i++)
                {
                    if (ChildKeys[i] == default(TKey))
                        throw new BplusTreeException("null child in first half");
                }
            }

            var result = ChildKeys[0];

            if (!IsLeaf)
            {
                MaterializeNodeAtIndex(0);
                result = MaterializedChildNodes[0].SanityCheck(visited);
                for (int i = 0; i < _size; i++)
                {
                    if (ChildKeys[i] == default(TKey))
                        break;
                    
                    MaterializeNodeAtIndex(i + 1);

                    var least = MaterializedChildNodes[i + 1].SanityCheck(visited);
                    
                    if (least == default(TKey))
                        throw new BplusTreeException("null least in child doesn't match node entry " + ChildKeys[i]);
                    
                    if (!least.Equals(ChildKeys[i]))
                        throw new BplusTreeException("least in child " + least + " doesn't match node entry " +
                                                     ChildKeys[i]);
                    
                }
            }

            // look for duplicate keys
            var lastkey = ChildKeys[0];

            for (int i = 1; i < _size; i++)
            {
                if (ChildKeys[i] == default(TKey))
                    break;
                
                if (lastkey.Equals(ChildKeys[i]))
                    throw new BplusTreeException("duplicate key in node " + lastkey);
                
                lastkey = ChildKeys[i];
            }

            return result;
        }

        private void Destroy()
        {
            // make sure the structure is useless, it should no longer be used.
            _owner = null;
            _parent = null;
            _size = -100;
            ChildBufferNumbers = null;
            ChildKeys = null;
            MaterializedChildNodes = null;
            MyBufferNumber = BplusTreeLong<TKey>.Nullbuffernumber;
            _indexInParent = -100;
            _dirty = false;
        }

        public int SizeInUse()
        {
            int result = 0;
            
            for (int i = 0; i < _size; i++)
            {
                if (ChildKeys[i] == default(TKey))
                    break;
                
                result++;
            }
            
            return result;
        }

        public static BplusNode<TKey> BinaryRoot(BplusNode<TKey> leftNode, TKey key, BplusNode<TKey> rightNode, BplusTreeLong<TKey> owner)
        {
            var newRoot = new BplusNode<TKey>(owner, null, -1, false);

            //newRoot.Clear(); // redundant
            newRoot.ChildKeys[0] = key;
            leftNode.Reparent(newRoot, 0);
            rightNode.Reparent(newRoot, 1);

            // new root is stored elsewhere
            return newRoot;
        }

        private void Reparent(BplusNode<TKey> newParent, int parentIndex)
        {
            // keys and existing parent structure must be updated elsewhere.
            _parent = newParent;
            _indexInParent = parentIndex;
            newParent.ChildBufferNumbers[parentIndex] = MyBufferNumber;
            newParent.MaterializedChildNodes[parentIndex] = this;

            // parent is no longer terminal
            _owner.ForgetTerminalNode(_parent);
        }

        private void Clear()
        {
            ChildBufferNumbers = new long[_size + 1];
            ChildKeys = new TKey[_size];
            MaterializedChildNodes = new BplusNode<TKey>[_size + 1];

            for (int i = 0; i < _size; i++)
            {
                ChildBufferNumbers[i] = BplusTreeLong <TKey>.Nullbuffernumber;
                MaterializedChildNodes[i] = null;
                ChildKeys[i] = default(TKey);
            }

            ChildBufferNumbers[_size] = BplusTreeLong <TKey>.Nullbuffernumber;
            MaterializedChildNodes[_size] = null;

            // this is now a terminal node
            _owner.RecordTerminalNode(this);
        }

        /// <summary>
        /// Find first index in self associated with a key same or greater than CompareKey
        /// </summary>
        /// <param name="compareKey">CompareKey</param>
        /// <param name="lookPastOnly">if true and this is a leaf then look for a greater value</param>
        /// <returns>lowest index of same or greater key or this.Size if no greater key.</returns>
        private int FindAtOrNextPosition(TKey compareKey, bool lookPastOnly)
        {
            int insertposition = 0;

            if (IsLeaf && !lookPastOnly)
            {
                // look for exact match or greater or null
                while (insertposition < _size 
                    && ChildKeys[insertposition] != null 
                    && ChildKeys[insertposition].CompareTo(compareKey) < 0)
                {
                    insertposition++;
                }
            }
            else
            {
                // look for greater or null only
                while (
                    insertposition < _size && 
                    ChildKeys[insertposition] != null &&
                    ChildKeys[insertposition].CompareTo(compareKey) <= 0)
                {
                    insertposition++;
                }
            }

            return insertposition;
        }

        /// <summary>
        /// Find the first key below atIndex, or if no such node traverse to the next key to the right.
        /// If no such key exists, return nulls.
        /// </summary>
        /// <param name="atIndex">where to look in this node</param>
        /// <param name="foundInLeaf">leaf where found</param>
        /// <param name="keyFound">key value found</param>
        private void TraverseToFollowingKey(int atIndex, out BplusNode<TKey> foundInLeaf, out TKey keyFound)
        {
            foundInLeaf = null;
            keyFound = default(TKey);
            
            var lookInParent = IsLeaf
                               ? (atIndex >= _size) || (ChildKeys[atIndex] == default(TKey))
                               : (atIndex > _size) || (atIndex > 0 && ChildKeys[atIndex - 1] == default(TKey));

            if (lookInParent)
            {
                // if it's anywhere it's in the next child of parent
                if (_parent != null && _indexInParent >= 0)
                {
                    _parent.TraverseToFollowingKey(_indexInParent + 1, out foundInLeaf, out keyFound);
                    return;
                }

                return; // no such following key
            }
            if (IsLeaf)
            {
                // leaf, we found it.
                foundInLeaf = this;
                keyFound = ChildKeys[atIndex];
                return;
            }
            
            // nonleaf, look in child (if there is one)
            if (atIndex != 0 && ChildKeys[atIndex - 1] == default(TKey)) 
                return;

            var thechild = MaterializeNodeAtIndex(atIndex);
            thechild.TraverseToFollowingKey(0, out foundInLeaf, out keyFound);
        }

        public bool  FindMatch(TKey compareKey, out long valueFound)
        {
            valueFound = 0; // dummy value on failure
            BplusNode<TKey> leaf;
            var position = FindAtOrNextPositionInLeaf(compareKey, out leaf, false);
            if (position < leaf._size)
            {
                var key = leaf.ChildKeys[position];
                if (key != null && key.CompareTo(compareKey) == 0) //(key.Equals(CompareKey)
                {
                    valueFound = leaf.ChildBufferNumbers[position];
                    return true;
                }
            }
            return false;
        }

        public bool UpdateMatch(TKey compareKey, long value)
        {
            BplusNode<TKey> leaf;
            var position = FindAtOrNextPositionInLeaf(compareKey, out leaf, false);
            if (position < leaf._size)
            {
                var key = leaf.ChildKeys[position];
                if (key != null && key.CompareTo(compareKey) == 0) //(key.Equals(CompareKey)
                {
                    leaf.ChildBufferNumbers[position] = value;
                    leaf.MarkAsDirty();
                    return true;
                }
            }
            return false;
        }
        public TKey FindNextKey(TKey compareKey)
        {
            
            BplusNode<TKey> leaf;
            var position = FindAtOrNextPositionInLeaf(compareKey, out leaf, true);

            TKey result;

            if (position >= leaf._size || leaf.ChildKeys[position] == null)
            {
                // try to traverse to the right.
                BplusNode<TKey> newleaf;
                leaf.TraverseToFollowingKey(leaf._size, out newleaf, out result);
            }
            else
            {
                result = leaf.ChildKeys[position];
            }

            return result;
        }

        /// <summary>
        /// Find near-index of comparekey in leaf under this node. 
        /// </summary>
        /// <param name="compareKey">the key to look for</param>
        /// <param name="inLeaf">the leaf where found</param>
        /// <param name="lookPastOnly">If true then only look for a greater value, not an exact match.</param>
        /// <returns>index of match in leaf</returns>
        private int FindAtOrNextPositionInLeaf(TKey compareKey, out BplusNode<TKey> inLeaf, bool lookPastOnly)
        {
            var myposition = FindAtOrNextPosition(compareKey, lookPastOnly);
            
            if (IsLeaf)
            {
                inLeaf = this;
                return myposition;
            }
            
            var childBufferNumber = ChildBufferNumbers[myposition];
            if (childBufferNumber == BplusTreeLong <TKey>.Nullbuffernumber)
                throw new BplusTreeException("can't search null subtree");

            var child = MaterializeNodeAtIndex(myposition);

            return child.FindAtOrNextPositionInLeaf(compareKey, out inLeaf, lookPastOnly);
        }

        private BplusNode<TKey> MaterializeNodeAtIndex(int myposition)
        {
            if (IsLeaf)
                throw new BplusTreeException("cannot materialize child for leaf");

            var childBufferNumber = ChildBufferNumbers[myposition];
            if (childBufferNumber == BplusTreeLong <TKey>.Nullbuffernumber)
                throw new BplusTreeException("can't search null subtree at position " + myposition + " in " + MyBufferNumber);

            // is it already materialized?
            var result = MaterializedChildNodes[myposition];
            if (result != null)
                return result;

            // otherwise read it in...
            result = new BplusNode<TKey>(_owner, this, myposition, true); // dummy isLeaf value
            result.LoadFromBuffer(childBufferNumber);
            MaterializedChildNodes[myposition] = result;

            // no longer terminal
            _owner.ForgetTerminalNode(this);

            return result;
        }

        public void LoadFromBuffer(long bufferNumber)
        {
            // freelist bookkeeping done elsewhere
            //var parentinfo = "no parent"; // debug
            //if (_parent != null)
            //{
            //    parentinfo = "parent=" + _parent.MyBufferNumber; // debug
            //}

            //System.Diagnostics.Debug.WriteLine("\r\n<br> loading "+this.indexInParent+" from "+bufferNumber+" for "+parentinfo);
            
            var rawdata = new byte[_owner.Buffersize];
            _owner.Buffers.GetBuffer(bufferNumber, rawdata, 0, rawdata.Length);
            Load(rawdata);
            _dirty = false;
            
            MyBufferNumber = bufferNumber;
            
            // it's terminal until a child is materialized
            _owner.RecordTerminalNode(this);
        }

        public long DumpToFreshBuffer()
        {
            var oldbuffernumber = MyBufferNumber;
            var freshBufferNumber = _owner.AllocateBuffer();

            //System.Diagnostics.Debug.WriteLine("\r\n<br> dumping "+this.indexInParent+" from "+oldbuffernumber+" to "+freshBufferNumber);
            DumpToBuffer(freshBufferNumber);

            if (oldbuffernumber != BplusTreeLong <TKey>.Nullbuffernumber)
            {
                //this.owner.FreeBuffersOnCommit.Add(oldbuffernumber);
                if (_owner.FreeBuffersOnAbort.Contains(oldbuffernumber))
                {
                    // free it now
                    _owner.FreeBuffersOnAbort.Remove(oldbuffernumber);
                    _owner.DeallocateBuffer(oldbuffernumber);
                }
                else
                {
                    // free on commit
                    _owner.FreeBuffersOnCommit.Add(oldbuffernumber);
                }
            }

            //this.owner.FreeBuffersOnAbort.Add(freshBufferNumber);
            _owner.FreeBuffersOnAbort.Add(freshBufferNumber);
            return freshBufferNumber;
        }

        private void DumpToBuffer(long buffernumber)
        {
            var rawdata = new byte[_owner.Buffersize];
            Dump(rawdata);
            _owner.Buffers.SetBuffer(buffernumber, rawdata, 0, rawdata.Length);
            _dirty = false;
            MyBufferNumber = buffernumber;
            if (_parent == null || _indexInParent < 0 || _parent.ChildBufferNumbers[_indexInParent] == buffernumber)
                return;

            if (_parent.MaterializedChildNodes[_indexInParent] != this)
                throw new BplusTreeException("invalid parent connection " + _parent.MyBufferNumber + " at " + _indexInParent);

            _parent.ChildBufferNumbers[_indexInParent] = buffernumber;
            _parent.MarkAsDirty();
        }

        private void ReParentAllChildren()
        {
            for (int i = 0; i <= _size; i++)
            {
                var thisnode = MaterializedChildNodes[i];

                if (thisnode != null)
                    thisnode.Reparent(this, i);
            }
        }

        /// <summary>
        /// Delete entry for key
        /// </summary>
        /// <param name="key">key to delete</param>
        /// <param name="mergeMe">true if the node is less than half full after deletion</param>
        /// <returns>null unless the smallest key under this node has changed in which case it returns the smallest key.</returns>
        public TKey Delete(TKey key, out bool mergeMe)
        {
            mergeMe = false; // assumption
            var result = default(TKey);

            if (IsLeaf)
                return DeleteLeaf(key, out mergeMe);

            int deleteposition = FindAtOrNextPosition(key, false);
            long deleteBufferNumber = ChildBufferNumbers[deleteposition];
            if (deleteBufferNumber == BplusTreeLong<TKey>.Nullbuffernumber)
                throw new BplusTreeException("key not followed by buffer number in non-leaf (del)");

            // del in subtree
            var deleteChild = MaterializeNodeAtIndex(deleteposition);
            bool mergeKid;
            var delresult = deleteChild.Delete(key, out mergeKid);

            // delete succeeded... now fix up the child node if needed.
            MarkAsDirty(); // redundant ?

            // bizarre special case for 2-3  or 3-4 trees -- empty leaf
            if (delresult != null && delresult.CompareTo(key) == 0)
            {
                if (_size > 3)
                    throw new BplusTreeException(
                        "assertion error: delete returned delete key for too large node size: " + _size);

                // junk this leaf and shift everything over
                if (deleteposition == 0)
                {
                    result = ChildKeys[deleteposition];
                }
                else if (deleteposition == _size)
                {
                    ChildKeys[deleteposition - 1] = default(TKey);
                }
                else
                {
                    ChildKeys[deleteposition - 1] = ChildKeys[deleteposition];
                }
                
                if (result != null
                    && result.CompareTo(key) == 0) // result.Equals(key)
                {
                    // I'm not sure this ever happens
                    MaterializeNodeAtIndex(1);
                    result = MaterializedChildNodes[1].LeastKey();
                }

                deleteChild.Free();
                for (int i = deleteposition; i < _size - 1; i++)
                {
                    ChildKeys[i] = ChildKeys[i + 1];
                    MaterializedChildNodes[i] = MaterializedChildNodes[i + 1];
                    ChildBufferNumbers[i] = ChildBufferNumbers[i + 1];
                }
                
                ChildKeys[_size - 1] = default(TKey);
                
                if (deleteposition < _size)
                {
                    MaterializedChildNodes[_size - 1] = MaterializedChildNodes[_size];
                    ChildBufferNumbers[_size - 1] = ChildBufferNumbers[_size];
                }
                
                MaterializedChildNodes[_size] = null;
                
                ChildBufferNumbers[_size] = BplusTreeLong<TKey>.Nullbuffernumber;
                mergeMe = (SizeInUse() < _size/2);
                ReParentAllChildren();
                
                return result;
            }

            if (deleteposition == 0)
            {
                result = delresult; // smallest key may have changed.
            }
            else if (
                (delresult != null && deleteposition > 0) 
                && delresult.CompareTo(key) != 0) // !delresult.Equals(key)
            {
                ChildKeys[deleteposition - 1] = delresult;
            }

            // if the child needs merging... do it
            if (mergeKid)
            {
                int leftindex, rightindex;
                BplusNode<TKey> leftNode;
                BplusNode<TKey> rightNode;
                if (deleteposition == 0)
                {
                    // merge with next
                    leftindex = deleteposition;
                    rightindex = deleteposition + 1;
                    leftNode = deleteChild;
                    //keyBetween = this.ChildKeys[deleteposition];
                    rightNode = MaterializeNodeAtIndex(rightindex);
                }
                else
                {
                    // merge with previous
                    leftindex = deleteposition - 1;
                    rightindex = deleteposition;
                    leftNode = MaterializeNodeAtIndex(leftindex);
                    //keyBetween = this.ChildKeys[deleteBufferNumber-1];
                    rightNode = deleteChild;
                }

                var keyBetween = ChildKeys[leftindex];
                TKey rightLeastKey;
                bool deleteRight;

                Merge(leftNode, keyBetween, rightNode, out rightLeastKey, out deleteRight);

                // delete the right node if needed.
                if (deleteRight)
                {
                    for (int i = rightindex; i < _size; i++)
                    {
                        ChildKeys[i - 1] = ChildKeys[i];
                        ChildBufferNumbers[i] = ChildBufferNumbers[i + 1];
                        MaterializedChildNodes[i] = MaterializedChildNodes[i + 1];
                    }

                    ChildKeys[_size - 1] = default(TKey);
                    MaterializedChildNodes[_size] = default(BplusNode<TKey>);
                    ChildBufferNumbers[_size] = BplusTreeLong <TKey>.Nullbuffernumber;
                    ReParentAllChildren();
                    rightNode.Free();

                    // does this node need merging?
                    if (SizeInUse() < _size/2)
                        mergeMe = true;
                }
                else
                {
                    // update the key entry
                    ChildKeys[rightindex - 1] = rightLeastKey;
                }
            }
            return result;
        }

        private TKey LeastKey()
        {
            TKey result;
            if (IsLeaf)
            {
                result = ChildKeys[0];
            }
            else
            {
                MaterializeNodeAtIndex(0);
                result = MaterializedChildNodes[0].LeastKey();
            }
            
            if (result == default(TKey))
                throw new BplusTreeException("no key found");
            
            return result;
        }

        public static void Merge(BplusNode<TKey> left, TKey keyBetween, BplusNode<TKey> right, out TKey rightLeastKey, out bool deleteRight)
        {
            //System.Diagnostics.Debug.WriteLine("\r\n<br> merging "+right.myBufferNumber+" ("+KeyBetween+") "+left.myBufferNumber);
            //System.Diagnostics.Debug.WriteLine(left.owner.toHtml());
            rightLeastKey = default(TKey); // only if DeleteRight
            if (left.IsLeaf || right.IsLeaf)
            {
                if (!(left.IsLeaf && right.IsLeaf))
                    throw new BplusTreeException("can't merge leaf with non-leaf");

                MergeLeaves(left, right, out deleteRight);
                rightLeastKey = right.ChildKeys[0];
                return;
            }

            // merge non-leaves
            deleteRight = false;
            var allkeys = new TKey[left._size * 2 + 1];
            var allseeks = new long[left._size * 2 + 2];
            var allMaterialized = new BplusNode<TKey>[left._size*2 + 2];

            if (left.ChildBufferNumbers[0] == BplusTreeLong<TKey>.Nullbuffernumber || right.ChildBufferNumbers[0] == BplusTreeLong<TKey>.Nullbuffernumber)
                throw new BplusTreeException("cannot merge empty non-leaf with non-leaf");

            int index = 0;
            allseeks[0] = left.ChildBufferNumbers[0];
            allMaterialized[0] = left.MaterializedChildNodes[0];
            for (int i = 0; i < left._size; i++)
            {
                if (left.ChildKeys[i] == null)
                {
                    break;
                }
                allkeys[index] = left.ChildKeys[i];
                allseeks[index + 1] = left.ChildBufferNumbers[i + 1];
                allMaterialized[index + 1] = left.MaterializedChildNodes[i + 1];
                index++;
            }

            allkeys[index] = keyBetween;
            index++;
            allseeks[index] = right.ChildBufferNumbers[0];
            allMaterialized[index] = right.MaterializedChildNodes[0];
            
            int rightcount = 0;
            for (int i = 0; i < right._size; i++)
            {
                if (right.ChildKeys[i] == default(TKey))
                    break;
                
                allkeys[index] = right.ChildKeys[i];
                allseeks[index + 1] = right.ChildBufferNumbers[i + 1];
                allMaterialized[index + 1] = right.MaterializedChildNodes[i + 1];
                index++;
                rightcount++;
            }

            if (index <= left._size)
            {
                // it will all fit in one node
                //System.Diagnostics.Debug.WriteLine("deciding to forget "+right.myBufferNumber+" into "+left.myBufferNumber);
                deleteRight = true;
                for (int i = 0; i < index; i++)
                {
                    left.ChildKeys[i] = allkeys[i];
                    left.ChildBufferNumbers[i] = allseeks[i];
                    left.MaterializedChildNodes[i] = allMaterialized[i];
                }

                left.ChildBufferNumbers[index] = allseeks[index];
                left.MaterializedChildNodes[index] = allMaterialized[index];
                left.ReParentAllChildren();
                left.MarkAsDirty();
                right.Free();

                return;
            }

            // otherwise split the content between the nodes
            left.Clear();
            right.Clear();
            left.MarkAsDirty();
            right.MarkAsDirty();

            int leftcontent = index/2;
            int rightcontent = index - leftcontent - 1;
            //rightLeastKey = allkeys[leftcontent];
            var outputindex = 0;
            for (int i = 0; i < leftcontent; i++)
            {
                left.ChildKeys[i] = allkeys[outputindex];
                left.ChildBufferNumbers[i] = allseeks[outputindex];
                left.MaterializedChildNodes[i] = allMaterialized[outputindex];
                outputindex++;
            }

            rightLeastKey = allkeys[outputindex];
            left.ChildBufferNumbers[outputindex] = allseeks[outputindex];
            left.MaterializedChildNodes[outputindex] = allMaterialized[outputindex];
            outputindex++;
            rightcount = 0;
            for (int i = 0; i < rightcontent; i++)
            {
                right.ChildKeys[i] = allkeys[outputindex];
                right.ChildBufferNumbers[i] = allseeks[outputindex];
                right.MaterializedChildNodes[i] = allMaterialized[outputindex];
                outputindex++;
                rightcount++;
            }

            right.ChildBufferNumbers[rightcount] = allseeks[outputindex];
            right.MaterializedChildNodes[rightcount] = allMaterialized[outputindex];
            left.ReParentAllChildren();
            right.ReParentAllChildren();
        }

        public static void MergeLeaves(BplusNode<TKey> left, BplusNode<TKey> right, out bool deleteRight)
        {
            deleteRight = false;
            
            var allkeys = new TKey[left._size * 2];
            var allseeks = new long[left._size*2];
            
            int index = 0;
            for (int i = 0; i < left._size; i++)
            {
                if (left.ChildKeys[i] == default(TKey))
                    break;

                allkeys[index] = left.ChildKeys[i];
                allseeks[index] = left.ChildBufferNumbers[i];
                index++;
            }

            for (int i = 0; i < right._size; i++)
            {
                if (right.ChildKeys[i] == default(TKey))
                    break;

                allkeys[index] = right.ChildKeys[i];
                allseeks[index] = right.ChildBufferNumbers[i];
                
                index++;
            }

            if (index <= left._size)
            {
                left.Clear();
                deleteRight = true;
                for (int i = 0; i < index; i++)
                {
                    left.ChildKeys[i] = allkeys[i];
                    left.ChildBufferNumbers[i] = allseeks[i];
                }
                right.Free();
                left.MarkAsDirty();
                return;
            }

            left.Clear();
            right.Clear();
            
            left.MarkAsDirty();
            right.MarkAsDirty();
            
            int rightcontent = index/2;
            int leftcontent = index - rightcontent;
            int newindex = 0;
            
            for (int i = 0; i < leftcontent; i++)
            {
                left.ChildKeys[i] = allkeys[newindex];
                left.ChildBufferNumbers[i] = allseeks[newindex];
                newindex++;
            }
            
            for (int i = 0; i < rightcontent; i++)
            {
                right.ChildKeys[i] = allkeys[newindex];
                right.ChildBufferNumbers[i] = allseeks[newindex];
                newindex++;
            }
        }

        public TKey DeleteLeaf(TKey key, out bool mergeMe)
        {
            var result = default(TKey);
            mergeMe = false;
            var found = false;
            var deletelocation = 0;

            foreach (var thiskey in ChildKeys)
            {
                // use comparison, not equals, in case different strings sometimes compare same
                if (
                    thiskey != null
                    && thiskey.CompareTo(key) == 0) // thiskey.Equals(key)
                {
                    found = true;
                    break;
                }

                deletelocation++;
            }
            
            if (!found)
                throw new BplusTreeKeyMissingException("cannot delete missing key: " + key);

            MarkAsDirty();

            // only keys are important...
            for (int i = deletelocation; i < _size - 1; i++)
            {
                ChildKeys[i] = ChildKeys[i + 1];
                ChildBufferNumbers[i] = ChildBufferNumbers[i + 1];
            }

            ChildKeys[_size - 1] = default(TKey);
            
            //this.MaterializedChildNodes[endlocation+1] = null;
            //this.ChildBufferNumbers[endlocation+1] = BplusTreeLong.NULLBUFFERNUMBER;
            
            if (SizeInUse() < _size/2)
                mergeMe = true;

            // the null case is only relevant for the case of 2-3 trees (empty leaf after deletion)
            if (deletelocation == 0)
                result = ChildKeys[0] ?? key;

            return result;
        }

        /// <summary>
        /// insert key/position entry in self 
        /// </summary>
        /// <param name="key">Key to associate with the leaf</param>
        /// <param name="position">position associated with key in external structur</param>
        /// <param name="splitString">if not null then the smallest key in the new split leaf</param>
        /// <param name="splitNode">if not null then the node was split and this is the leaf to the right.</param>
        /// <returns>null unless the smallest key under this node has changed, in which case it returns the smallest key.</returns>
        public TKey Insert(TKey key, long position, out TKey splitString, out BplusNode<TKey> splitNode)
        {
            if (IsLeaf)
                return InsertLeaf(key, position, out splitString, out splitNode);

            splitString = default(TKey);
            splitNode = null;
            
            int insertposition = FindAtOrNextPosition(key, false);
            long insertBufferNumber = ChildBufferNumbers[insertposition];

            if (insertBufferNumber == BplusTreeLong < TKey>.Nullbuffernumber)
                throw new BplusTreeException("key not followed by buffer number in non-leaf");

            // insert in subtree
            var insertChild = MaterializeNodeAtIndex(insertposition);
            
            BplusNode<TKey> childSplit;
            TKey childSplitString;
            var childInsert = insertChild.Insert(key, position, out childSplitString, out childSplit);

            // if there was a split the node must expand
            if (childSplit != null)
                // insert the child
                splitNode = ExpandNode(insertposition, childSplit, childSplitString, out splitString);

            return insertposition == 0 
                ? childInsert 
                : default(TKey);
        }

        private BplusNode<TKey> ExpandNode(int insertposition, BplusNode<TKey> childSplit, TKey childSplitString, out TKey splitString)
        {
            splitString = default(TKey);

            BplusNode<TKey> splitNode = default(BplusNode<TKey>);
            MarkAsDirty(); // redundant -- a child will have a change so this node will need to be copied
            var newChildPosition = insertposition + 1;
            var dosplit = false;

            // if there is no free space we must do a split
            if (ChildBufferNumbers[_size] != BplusTreeLong<TKey>.Nullbuffernumber)
            {
                dosplit = true;
                PrepareForSplit();
            }

            // bubble over the current values to make space for new child
            for (int i = ChildKeys.Length - 2; i >= newChildPosition - 1; i--)
            {
                int i1 = i + 1;
                int i2 = i1 + 1;
                ChildKeys[i1] = ChildKeys[i];
                ChildBufferNumbers[i2] = ChildBufferNumbers[i1];
                MaterializedChildNodes[i2] = MaterializedChildNodes[i1];
            }
                
            // record the new child
            ChildKeys[newChildPosition - 1] = childSplitString;

            //this.MaterializedChildNodes[newChildPosition] = childSplit;
            //this.ChildBufferNumbers[newChildPosition] = childSplit.myBufferNumber;

            childSplit.Reparent(this, newChildPosition);

            // split, if needed
            if (dosplit)
            {
                splitNode = SplitNode(out splitString);
            }
            // fix pointers in children
            ReParentAllChildren();
            
            return splitNode;
        }

        private BplusNode<TKey> SplitNode(out TKey splitString)
        {
            int splitpoint = MaterializedChildNodes.Length/2 - 1;
            splitString = ChildKeys[splitpoint];
            var splitNode = new BplusNode<TKey>(_owner, _parent, -1, IsLeaf);

            // make copy of expanded node structure
            BplusNode<TKey>[] materialized = MaterializedChildNodes;
            long[] buffernumbers = ChildBufferNumbers;
            TKey[] keys = ChildKeys;

            // repair the expanded node
            ChildKeys = new TKey[_size];
            MaterializedChildNodes = new BplusNode<TKey>[_size + 1];
            ChildBufferNumbers = new long[_size + 1];
            Clear();

            Array.Copy(materialized, 0, MaterializedChildNodes, 0, splitpoint + 1);
            Array.Copy(buffernumbers, 0, ChildBufferNumbers, 0, splitpoint + 1);
            Array.Copy(keys, 0, ChildKeys, 0, splitpoint);

            // initialize the new node
            splitNode.Clear(); // redundant.
            int remainingKeys = _size - splitpoint;
            Array.Copy(materialized, splitpoint + 1, splitNode.MaterializedChildNodes, 0, remainingKeys + 1);
            Array.Copy(buffernumbers, splitpoint + 1, splitNode.ChildBufferNumbers, 0, remainingKeys + 1);
            Array.Copy(keys, splitpoint + 1, splitNode.ChildKeys, 0, remainingKeys);

            // fix pointers in materialized children of splitnode
            splitNode.ReParentAllChildren();

            // store the new node
            splitNode.DumpToFreshBuffer();
            splitNode.CheckIfTerminal();
            splitNode.MarkAsDirty();
            CheckIfTerminal();

            return splitNode;
        }

        /// <summary>
        /// Check to see if this is a terminal node, if so record it, otherwise forget it
        /// </summary>
        private void CheckIfTerminal()
        {
            if (!IsLeaf)
            {
                for (int i = 0; i < _size + 1; i++)
                {
                    if (MaterializedChildNodes[i] != null)
                    {
                        _owner.ForgetTerminalNode(this);
                        return;
                    }
                }
            }
            _owner.RecordTerminalNode(this);
        }

        /// <summary>
        /// insert key/position entry in self (as leaf)
        /// </summary>
        /// <param name="key">Key to associate with the leaf</param>
        /// <param name="position">position associated with key in external structure</param>
        /// <param name="splitString">if not null then the smallest key in the new split leaf</param>
        /// <param name="splitNode">if not null then the node was split and this is the leaf to the right.</param>
        /// <returns>smallest key value in keys, or null if no change</returns>
        public TKey InsertLeaf(TKey key, long position, out TKey splitString, out BplusNode<TKey> splitNode)
        {
            splitString = default(TKey);
            splitNode = null;
            var dosplit = false;
            if (!IsLeaf)
                throw new BplusTreeException("bad call to InsertLeaf: this is not a leaf");

            MarkAsDirty();

            var insertposition = FindAtOrNextPosition(key, false);
            if (insertposition >= _size)
            {
                //throw new BplusTreeException("key too big and leaf is full");
                dosplit = true;
                PrepareForSplit();
            }
            else
            {
                // if it's already there then change the value at the current location (duplicate entries not supported).
                if (ChildKeys[insertposition] == null
                    || ChildKeys[insertposition].CompareTo(key) == 0)
                    // this.ChildKeys[insertposition].Equals(key)
                {
                    ChildBufferNumbers[insertposition] = position;
                    ChildKeys[insertposition] = key;
                    return insertposition == 0 ? key : default(TKey);
                }
            }

            // check for a null position
            int nullindex = insertposition;
            while (nullindex < ChildKeys.Length && ChildKeys[nullindex] != null)
            {
                nullindex++;
            }

            if (nullindex >= ChildKeys.Length)
            {
                if (dosplit)
                    throw new BplusTreeException("can't split twice!!");

                //throw new BplusTreeException("no space in leaf");
                dosplit = true;
                PrepareForSplit();
            }

            // bubble in the new info XXXX THIS SHOULD BUBBLE BACKWARDS	
            var nextkey = ChildKeys[insertposition];
            var nextposition = ChildBufferNumbers[insertposition];
            
            ChildKeys[insertposition] = key;
            ChildBufferNumbers[insertposition] = position;

            while (nextkey != default(TKey))
            {
                key = nextkey;
                position = nextposition;
                insertposition++;
                nextkey = ChildKeys[insertposition];
                nextposition = ChildBufferNumbers[insertposition];
                ChildKeys[insertposition] = key;
                ChildBufferNumbers[insertposition] = position;
            }

            // split if needed
            if (dosplit)
            {
                int splitpoint = ChildKeys.Length/2;
                int splitlength = ChildKeys.Length - splitpoint;
                splitNode = new BplusNode<TKey>(_owner, _parent, -1, IsLeaf);

                // copy the split info into the splitNode
                Array.Copy(ChildBufferNumbers, splitpoint, splitNode.ChildBufferNumbers, 0, splitlength);
                Array.Copy(ChildKeys, splitpoint, splitNode.ChildKeys, 0, splitlength);
                Array.Copy(MaterializedChildNodes, splitpoint, splitNode.MaterializedChildNodes, 0, splitlength);
                
                splitString = splitNode.ChildKeys[0];
                
                // archive the new node
                splitNode.DumpToFreshBuffer();
                
                // store the node data temporarily
                long[] buffernumbers = ChildBufferNumbers;
                TKey[] keys = ChildKeys;
                BplusNode<TKey>[] nodes = MaterializedChildNodes;
                
                // repair current node, copy in the other part of the split
                ChildBufferNumbers = new long[_size + 1];
                ChildKeys = new TKey[_size];
                MaterializedChildNodes = new BplusNode<TKey>[_size + 1];

                Array.Copy(buffernumbers, 0, ChildBufferNumbers, 0, splitpoint);
                Array.Copy(keys, 0, ChildKeys, 0, splitpoint);
                Array.Copy(nodes, 0, MaterializedChildNodes, 0, splitpoint);
                
                for (int i = splitpoint; i < ChildKeys.Length; i++)
                {
                    ChildKeys[i] = default(TKey);
                    ChildBufferNumbers[i] = BplusTreeLong<TKey>.Nullbuffernumber;
                    MaterializedChildNodes[i] = null;
                }

                // store the new node
                //splitNode.DumpToFreshBuffer();
                _owner.RecordTerminalNode(splitNode);
                splitNode.MarkAsDirty();
            }

            //return this.ChildKeys[0];
            return insertposition == 0 ? key : default(TKey);
        }

        /// <summary>
        /// Grow to this.size+1 in preparation for insertion and split
        /// </summary>
        private void PrepareForSplit()
        {
            var supersize = _size + 1;
            var positions = new long[supersize + 1];
            var keys = new TKey[supersize];
            var materialized = new BplusNode<TKey>[supersize + 1];

            Array.Copy(ChildBufferNumbers, 0, positions, 0, _size + 1);
            positions[_size + 1] = BplusTreeLong<TKey>.Nullbuffernumber;

            Array.Copy(ChildKeys, 0, keys, 0, _size);
            keys[_size] = default(TKey);

            Array.Copy(MaterializedChildNodes, 0, materialized, 0, _size + 1);
            materialized[_size + 1] = null;
            
            ChildBufferNumbers = positions;
            ChildKeys = keys;
            
            MaterializedChildNodes = materialized;
        }

        public void Load(byte[] buffer)
        {
            // load serialized data
            // indicator | seek position | [ key storage | seek position ]*

            Clear();

            if (buffer.Length != _owner.Buffersize)
                throw new BplusTreeException("bad buffer size " + buffer.Length + " should be " + _owner.Buffersize);

            var indicator = buffer[0];

            IsLeaf = false;
            if (indicator == BplusTreeLong<TKey>.Leaf)
            {
                IsLeaf = true;
            }
            else if (indicator != BplusTreeLong<TKey>.Nonleaf)
            {
                throw new BplusTreeException("bad indicator, not leaf or nonleaf in tree " + indicator);
            }

            var index = 1;

            // get the first seek position
            ChildBufferNumbers[0] = ByteTools.RetrieveLong(buffer, index);

            // advance past next long
            index += ByteTools.LongStorage;
            
            var maxKeyLength = _owner.KeyLength;
            var maxKeyPayload = maxKeyLength - ByteTools.ShortStorage;

            //this.NumberOfValidKids = 0;
            // get remaining key storages and seek positions

            var lastKeyWasNull = false;
            
            for (int keyIndex = 0; keyIndex < _size; keyIndex++)
            {
                // decode and store a key
                short keylength = ByteTools.RetrieveShort(buffer, index);
                
                if (keylength < -1 || keylength > maxKeyPayload)
                    throw new BplusTreeException("invalid keylength decoded");

                index += ByteTools.ShortStorage;
                var key = default(TKey);
                
                if (keylength > 0)
                {
                    var keyBytes = new byte[keylength];

                    Array.Copy(buffer, index, keyBytes, 0, keylength);

                    key = _owner.KeyConverter.To(keyBytes);
                    
                    //var charCount = decode.GetCharCount(buffer, index, keylength);
                    //var ca = new char[charCount];
                    //decode.GetChars(buffer, index, keylength, ca, 0);
                    // //this.NumberOfValidKids++;
                    
                    // TODO: use converter...
                    //key = new String(ca);
                }

                ChildKeys[keyIndex] = key;
                index += maxKeyPayload;

                // decode and store a seek position
                var seekPosition = ByteTools.RetrieveLong(buffer, index);

                if (!IsLeaf)
                {
                    if (key == default(TKey) & seekPosition != BplusTreeLong<TKey>.Nullbuffernumber)
                        throw new BplusTreeException("key is null but position is not " + keyIndex);

                    if (lastKeyWasNull && key != default(TKey))
                        throw new BplusTreeException("null key followed by non-null key " + keyIndex);
                }

                var lastkey = key;
                lastKeyWasNull = lastkey == default(TKey);
                ChildBufferNumbers[keyIndex + 1] = seekPosition;
                index += ByteTools.LongStorage;
            }
        }

        /// <summary>
        /// check that key is ok for node of this size (put here for locality of relevant code).
        /// </summary>
        /// <param name="key">key to check</param>
        /// <param name="owner">tree to contain node containing the key</param>
        /// <returns>true if key is ok</returns>
        public static bool IsKeyValid(TKey key, BplusTreeLong<TKey> owner)
        {
            if (key == default(TKey))
                return false;

            //var encode = Encoding.Default.GetEncoder();// UTF8.GetEncoder();
            var maxKeyLength = owner.KeyLength;
            var maxKeyPayload = maxKeyLength - ByteTools.ShortStorage;

            var keyBytes = owner.KeyConverter.From(key);

            //var keyChars = key.ToCharArray();
            //var charCount = encode.GetByteCount(keyChars, 0, keyChars.Length, true);
            //return charCount <= maxKeyPayload;

            return keyBytes.Length <= maxKeyPayload;
        }

        public void Dump(byte[] buffer)
        {
            // indicator | seek position | [ key storage | seek position ]*
            if (buffer.Length != _owner.Buffersize)
                throw new BplusTreeException("bad buffer size " + buffer.Length + " should be " + _owner.Buffersize);

            buffer[0] = BplusTreeLong<TKey>.Nonleaf;
            if (IsLeaf)
                buffer[0] = BplusTreeLong<TKey>.Leaf;

            var index = 1;

            // store first seek position
            ByteTools.Store(ChildBufferNumbers[0], buffer, index);
            index += ByteTools.LongStorage;
            //var encode = Encoding.Default.GetEncoder();
                //Encoding.UTF8.GetEncoder();
            
            // store remaining keys and seeks
            var maxKeyLength = _owner.KeyLength;
            var maxKeyPayload = maxKeyLength - ByteTools.ShortStorage;
            var lastKeyWasNull = false;
            for (int keyIndex = 0; keyIndex < _size; keyIndex++)
            {
                // store a key
                var theKey = ChildKeys[keyIndex];
                short keyLength = -1;
            
                if (theKey != default(TKey))
                {
                    var keyBytes = _owner.KeyConverter.From(theKey);
                    keyLength = (short) keyBytes.Length;

                    //var keyChars = theKey.ToCharArray();
                    //charCount = (short) encode.GetByteCount(keyChars, 0, keyChars.Length, true);
                    
                    //if (charCount > maxKeyPayload)
                    //    throw new BplusTreeException("string bytes to large for use as key " + charCount + ">" + maxKeyPayload);

                    ByteTools.Store(keyLength, buffer, index);
                    index += ByteTools.ShortStorage;
                    
                    Buffer.BlockCopy(keyBytes, 0, buffer, index, keyLength);

                    //encode.GetBytes(keyChars, 0, keyChars.Length, buffer, index, true);
                }
                else
                {
                    // null case (no string to read)
                    ByteTools.Store(keyLength, buffer, index);
                    index += ByteTools.ShortStorage;
                }
                
                index += maxKeyPayload;
                
                // store a seek
                long seekPosition = ChildBufferNumbers[keyIndex + 1];

                if (theKey == default(TKey) && seekPosition != BplusTreeLong<TKey>.Nullbuffernumber && !IsLeaf)
                    throw new BplusTreeException("null key paired with non-null location " + keyIndex);

                if (lastKeyWasNull && theKey != default(TKey) )
                    throw new BplusTreeException("null key followed by non-null key " + keyIndex);
                
                var lastkey = theKey;
                lastKeyWasNull = lastkey == default(TKey);

                ByteTools.Store(seekPosition, buffer, index);
                index += ByteTools.LongStorage;
            }
        }

        /// <summary>
        /// Close the node:
        /// invalidate all children, store state if needed, remove materialized self from parent.
        /// </summary>
        public long Invalidate(bool destroyRoot)
        {
            var result = MyBufferNumber;
            if (!IsLeaf)
            {
                // need to invalidate kids
                for (int i = 0; i < _size + 1; i++)
                {
                    if (MaterializedChildNodes[i] == null) continue;

                    // new buffer numbers are recorded automatically
                    ChildBufferNumbers[i] = MaterializedChildNodes[i].Invalidate(true);
                }
            }

            // store if dirty
            if (_dirty)
            {
                result = DumpToFreshBuffer();
//				result = this.myBufferNumber;
            }

            // remove from owner archives if present
            _owner.ForgetTerminalNode(this);

            // remove from parent
            if (_parent != null && _indexInParent >= 0)
            {
                _parent.MaterializedChildNodes[_indexInParent] = null;
                _parent.ChildBufferNumbers[_indexInParent] = result; // should be redundant
                _parent.CheckIfTerminal();
                _indexInParent = -1;
            }

            // render all structures useless, just in case...
            if (destroyRoot)
                Destroy();

            return result;
        }

        /// <summary>
        /// Mark this as dirty and all ancestors too.
        /// </summary>
        private void MarkAsDirty()
        {
            if (_dirty)
                return; // don't need to do it again

            _dirty = true;

            if (_parent != null)
                _parent.MarkAsDirty();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            string dirtyState = "clean";

            if (_dirty)
                dirtyState = "dirty";

            var keycount = 0;
            if (IsLeaf)
            {
                for (int i = 0; i < _size; i++)
                {
                    var key = ChildKeys[i];
                    var seek = ChildBufferNumbers[i];

                    if (key == default(TKey)) continue;

                    var keyText = StringTools.PrintableString(key.ToString());
                    sb.AppendLine("'" + keyText + "' : " + seek + "<br>");
                    keycount++;
                }
                sb.AppendLine("leaf " + _indexInParent + " at " + MyBufferNumber + " #keys==" + keycount + " " + dirtyState);
            }
            else
            {
                sb.AppendLine("------------------------");
                sb.AppendLine("nonleaf " + _indexInParent + " at " + MyBufferNumber + " " + dirtyState);
                sb.AppendLine("------------------------");

                if (ChildBufferNumbers[0] != BplusTreeLong<TKey>.Nullbuffernumber)
                {
                    MaterializeNodeAtIndex(0);
                    sb.AppendLine(ChildBufferNumbers[0].ToString());
                    sb.Append(MaterializedChildNodes[0].ToString());
                    sb.AppendLine("------------------------");
                }

                for (int i = 0; i < _size; i++)
                {
                    var key = ChildKeys[i];
                    if (key == default(TKey)) break;

                    var keyText = StringTools.PrintableString(key.ToString());
                    sb.AppendLine("------------------------");
                    sb.AppendLine("'" + keyText + "'");
                    
                    try
                    {
                        MaterializeNodeAtIndex(i + 1);
                        sb.AppendLine("------------------------");
                        sb.AppendLine(ChildBufferNumbers[i + 1].ToString());
                        sb.Append(MaterializedChildNodes[i + 1].ToString());
                    }
                    catch (BplusTreeException)
                    {
                        sb.AppendLine("------------------------");
                        sb.Append("COULDN'T MATERIALIZE NODE " + (i + 1));
                    }
                    
                    keycount++;
                }

                sb.AppendLine("------------------------");
                sb.AppendLine("#keys==" + keycount);
                sb.AppendLine("------------------------");
            }

            return sb.ToString();
        }
    }
}