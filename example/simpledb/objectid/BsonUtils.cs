using System;
using System.Text;

namespace bsharptree.example.simpledb.objectid
{
    /// <summary>
    /// A static class containing BSON utility methods.
    /// </summary>
    public static class BsonUtils
    {
        /// <summary>
        /// Parses a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <returns>A byte array.</returns>
        public static byte[] ParseHexString(string s)
        {
            byte[] bytes;
            if (!TryParseHexString(s, out bytes))
            {
                string message = string.Format("'{0}' is not a valid hex string.", s);
                throw new FormatException(message);
            }
            return bytes;
        }

        /// <summary>
        /// Converts from number of milliseconds since Unix epoch to DateTime.
        /// </summary>
        /// <param name="millisecondsSinceEpoch">The number of milliseconds since Unix epoch.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime ToDateTimeFromMillisecondsSinceEpoch(long millisecondsSinceEpoch)
        {
            // MaxValue has to be handled specially to avoid rounding errors
            if (millisecondsSinceEpoch == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            
            return BsonConstants.UnixEpoch.AddTicks(millisecondsSinceEpoch*10000);
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length*2);
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a DateTime to local time (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <param name="kind">A DateTimeKind.</param>
        /// <returns>The DateTime in local time.</returns>
        public static DateTime ToLocalTime(DateTime dateTime, DateTimeKind kind)
        {
            if (dateTime.Kind == kind)
                return dateTime;

            if (dateTime == DateTime.MinValue)
                return DateTime.SpecifyKind(DateTime.MinValue, kind);

            if (dateTime == DateTime.MaxValue)
                return DateTime.SpecifyKind(DateTime.MaxValue, kind);

            return DateTime.SpecifyKind(dateTime.ToLocalTime(), kind);
        }

        /// <summary>
        /// Converts a DateTime to number of milliseconds since Unix epoch.
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>Number of seconds since Unix epoch.</returns>
        public static long ToMillisecondsSinceEpoch(DateTime dateTime)
        {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks/10000;
        }

        /// <summary>
        /// Converts a DateTime to UTC (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in UTC.</returns>
        public static DateTime ToUniversalTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;
            
            if (dateTime == DateTime.MinValue)
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            
            if (dateTime == DateTime.MaxValue)
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            
            return dateTime.ToUniversalTime();
        }

        /// <summary>
        /// Tries to parse a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="bytes">A byte array.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexString(string s, out byte[] bytes)
        {
            bytes = default(byte[]);
            
            if (string.IsNullOrEmpty(s))
                return false;

            // make length of s even
            if ((s.Length & 1) != 0)
                s = "0" + s;

            bytes = new byte[s.Length/2];
            
            for (int i = 0; i < bytes.Length; i++)
            {
                var hex = s.Substring(2*i, 2);
                
                try
                {
                    var b = Convert.ToByte(hex, 16);
                    bytes[i] = b;
                }
                catch (FormatException)
                {
                    bytes = default(byte[]);
                    return false;
                }
            }
            
            return true;
        }
    }
}