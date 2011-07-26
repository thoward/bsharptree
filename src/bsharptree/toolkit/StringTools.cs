using System;
using System.Text;

namespace bsharptree.toolkit
{
    public static class StringTools
    {
        public static string BytesToString(byte[] bytes)
        {
            return StringConverter.Default.To(bytes);
            //var decode =
            //    //Encoding.Default.GetDecoder();
            //    Encoding.UTF8.GetDecoder();
            //long length = decode.GetCharCount(bytes, 0, bytes.Length);
            //var chars = new char[length];
            //decode.GetChars(bytes, 0, bytes.Length, chars, 0);
            //var result = new String(chars);
            //return result;
        }

        public static byte[] StringToBytes(string thestring)
        {
            return StringConverter.Default.From(thestring);

            //var encode =
            //    //Encoding.Default.GetEncoder();
            //    Encoding.UTF8.GetEncoder();
            //var chars = thestring.ToCharArray();
            //long length = encode.GetByteCount(chars, 0, chars.Length, true);
            //var bytes = new byte[length];
            //encode.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            //return bytes;
        }

        public static string PrintableString(string s)
        {
            if (s == null)
                return "[NULL]";

            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c))
                    sb.Append(c);
                else
                    sb.Append("[" + Convert.ToInt32(c) + "]");
            }

            return sb.ToString();
        }
    }
}