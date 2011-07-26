using System;

namespace bsharptree.toolkit
{
    /// <summary>
    /// A simple class wrapper for Guid
    /// </summary>
    public class Guid: IFormattable, IComparable, IComparable<Guid>, IEquatable<Guid>
    {
        public static readonly Guid Empty = new Guid(System.Guid.Empty);

        public Guid()
        {
            _guid = new System.Guid();
        }

        public Guid(System.Guid guid)
        {
            _guid = guid;
        }
        
        public Guid(byte[] b) : this(new System.Guid(b)) { }
        
        public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
            : this(new System.Guid(a,b,c,d,e,f,g,h,i,j,k)) { }

        public Guid(int a, short b, short c, byte[] d)
            : this(new System.Guid(a, b, c, d)) { }
        
        public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
            : this(new System.Guid(a, b, c, d, e, f, g, h, i, j, k)) { }

        public Guid(string g)
            : this(new System.Guid(g)) { }

        private System.Guid _guid;

        public int CompareTo(object value)
        {
            return _guid.CompareTo(value);
        }

        public int CompareTo(Guid value)
        {
            return _guid.CompareTo(value);
        }

        public bool Equals(Guid g)
        {
            return _guid.Equals(g);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return _guid.ToString(format, provider);
        }

        public static bool operator ==(Guid a, Guid b)
        {
            return a._guid == b._guid;
        }
        public static bool operator !=(Guid a, Guid b)
        {
            return a._guid != b._guid;
        }
        public byte[] ToByteArray()
        {
            return _guid.ToByteArray();
        }

        public override string ToString()
        {
            return _guid.ToString();
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }
        public override bool Equals(object o)
        {
            return _guid.Equals(o);
        }

        public string ToString(string format)
        {
            return _guid.ToString(format);
        }

        public static implicit operator System.Guid(Guid guid)
        {
            return guid._guid;
        }
        public static implicit operator Guid(System.Guid guid)
        {
            return new Guid(guid);
        }
        public static Guid NewGuid()
        {
            return new Guid(System.Guid.NewGuid());
        }

        public static GenericConverter<Guid, byte[]> DefaultConverter =
            new GenericConverter<Guid, byte[]>(array => new Guid(array), byteCollection => byteCollection.ToByteArray());
    }
}