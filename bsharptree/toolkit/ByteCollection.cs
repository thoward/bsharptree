using System;

namespace bsharptree.toolkit
{
    /// <summary>
    /// A simple class wrapper for a byte[]
    /// </summary>
    public class ByteCollection : IEquatable<ByteCollection>, IComparable<ByteCollection>
    {
        public ByteCollection(byte[] data)
        {
            _data = data;
        }

        private byte[] _data;
        public byte[] Data { get { return _data; } set { _data = value; } }

        #region IEquatable<ByteCollection> Members

        public bool Equals(ByteCollection other)
        {
            if (ReferenceEquals(this, other)) return true;

            if (other == null) return false;

            if (ReferenceEquals(_data, other._data)) return true;

            if (other._data == null) return false;

            if (_data.Length != other._data.Length) return false;

            for (int i = 0; i < _data.Length; i++)
            {
                if (_data[i] != other._data[i]) return false;
            }
            return true;
        }

        #endregion

        #region IComparable<ByteCollection> Members

        public int CompareTo(ByteCollection other)
        {
            if (ReferenceEquals(this, other)) return 0;

            if (other == null) return 1;

            if (ReferenceEquals(_data, other._data)) return 0;

            if (other._data == null) return 1;

            if (_data.Length > other._data.Length) return 1;
            if (_data.Length < other._data.Length) return -1;

            for (int i = 0; i < _data.Length; i++)
            {
                int sub = _data[i] - other._data[i];
                if (sub > 0) return 1;
                if (sub < 0) return -1;
            }

            return 0;
        }

        //static bool ArraysEqual<T>(T[] a1, T[] a2)
        //{
        //    if (ReferenceEquals(a1, a2))
        //        return true;

        //    if (a1 == null || a2 == null)
        //        return false;

        //    if (a1.Length != a2.Length)
        //        return false;

        //    EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        //    for (int i = 0; i < a1.Length; i++)
        //    {
        //        if (!comparer.Equals(a1[i], a2[i])) return false;
        //    }
        //    return true;
        //}

        //static int ArraysCompare<T>(T[] a1, T[] a2)
        //{
        //    if (ReferenceEquals(a1, a2))
        //        return 0;

        //    if (a2 == null) return 1;
        //    if (a1 == null) return -1;

        //    if (a1.Length > a2.Length) return 1;
        //    if (a1.Length < a2.Length) return -1;

        //    EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        //    for (int i = 0; i < a1.Length; i++)
        //    {
        //        if (!comparer.Equals(a1[i], a2[i])) return false;
        //    }
        //    return true;
        //}

        #endregion

        public static GenericConverter<ByteCollection, byte[]> DefaultConverter =
            new GenericConverter<ByteCollection, byte[]>(a => new ByteCollection(a), a => a.Data);
    }
}