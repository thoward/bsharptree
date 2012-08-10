using System;

namespace bsharptree.example.simpledb.objectid
{
    /// <summary>
    /// A static class containing BSON constants.
    /// </summary>
    public static class BsonConstants
    {
        static BsonConstants()
        {
            // unixEpoch has to be initialized first
            UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTimeMaxValueMillisecondsSinceEpoch = (DateTime.MaxValue - UnixEpoch).Ticks/10000;
            DateTimeMinValueMillisecondsSinceEpoch = (DateTime.MinValue - UnixEpoch).Ticks/10000;
        }

        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MaxValue.
        /// </summary>
        public static long DateTimeMaxValueMillisecondsSinceEpoch { get; private set; }

        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MinValue.
        /// </summary>
        public static long DateTimeMinValueMillisecondsSinceEpoch { get; private set; }

        /// <summary>
        /// Gets the Unix Epoch for BSON DateTimes (1970-01-01).
        /// </summary>
        public static DateTime UnixEpoch { get; private set; }
    }
}