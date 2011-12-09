using System;

namespace bsharptree.definition
{
    /// <summary>
    /// This is the shared interface among the various tree index implementations.  
    /// </summary>
    public interface ITreeIndex<TKey, TValue> where TKey : class, IEquatable<TKey>, IComparable<TKey>
    {
        TValue this[TKey key] { get; set; }

        IConverter<TKey, byte[]> KeyConverter { get; set; }
        IConverter<TValue, byte[]> ValueConverter { get; set; }

        /// <summary>
        /// Examine the structure and optionally try to reclaim unreachable space.  A structure which was modified without a
        /// concluding commit or abort may contain unreachable space.
        /// </summary>
        /// <param name="correctErrors">if true try to correct errors detected, if false throw an exception on errors.</param>
        void Recover(bool correctErrors);

        /// <summary>
        /// Dispose of the key and its associated value.  Throw an exception if the key is missing.
        /// </summary>
        /// <param name="key">Key to erase.</param>
        void RemoveKey(TKey key);

        /// <summary>
        /// Get the least key in the structure.
        /// </summary>
        /// <returns>least key value or null if the tree is empty.</returns>
        TKey FirstKey();

        /// <summary>
        /// Get the least key in the structure strictly "larger" than the argument.  Return null if there is no such key.
        /// </summary>
        /// <param name="afterThisKey">The "lower limit" for the value to return</param>
        /// <returns>Least key greater than argument or null</returns>
        TKey NextKey(TKey afterThisKey);

        /// <summary>
        /// Return true if the key is present in the structure.
        /// </summary>
        /// <param name="key">Key to test</param>
        /// <returns>true if present, otherwise false.</returns>
        bool ContainsKey(TKey key);

        bool UpdateKey(TKey key, TValue value);

        /// <summary>
        /// Make changes since the last commit permanent.
        /// </summary>
        void Commit();

        /// <summary>
        /// Discard changes since the last commit and return to the state at the last commit point.
        /// </summary>
        void Abort();

        /// <summary>
        /// Set a parameter used to decide when to release memory mapped buffers.
        /// Larger values mean that more memory is used but accesses may be faster
        /// especially if there is locality of reference.  5 is too small and 1000
        /// may be too big.
        /// </summary>
        /// <param name="limit">maximum number of leaves with no materialized children to keep in memory.</param>
        void SetFootPrintLimit(int limit);

        /// <summary>
        /// Close and flush the streams without committing or aborting.
        /// (This is equivalent to abort, except unused space in the streams may be left unreachable).
        /// </summary>
        void Shutdown();

        ///// <summary>
        ///// Use the culture context for this tree to compare two strings.
        ///// </summary>
        ///// <param name="left"></param>
        ///// <param name="right"></param>
        ///// <returns></returns>
        //int Compare(TKey left, TKey right);
    }
}