namespace bsharptree.example.simpleindex.storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class StreamLoadingTools
    {
        public static Int32 ReadInt32(this Stream stream)
        {
            return BitConverter.ToInt32(stream.ReadBytes(4), 0);           
        }

        public static Int64 ReadInt64(this Stream stream)
        {
            return BitConverter.ToInt64(stream.ReadBytes(8), 0);
        }

        public static Guid ReadGuid(this Stream stream)
        {
            return new Guid(ReadBytes(stream, 16));
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return bytes;
        }
     
        public static IEnumerable<DocumentLocation> ReadDocumentLocations(this Stream stream)
        {
            // read guid
            var documentGuid = stream.ReadGuid();
            // read span count
            var spanCount = stream.ReadInt64();
            // read spans
            for (int i = 0; i < spanCount; i++)
            {
                var span = stream.ReadSpan();

                // yield pairs
                yield return new DocumentLocation{ Document = documentGuid, Span = span };
            }
        }
        public static Span ReadSpan(this Stream stream)
        {
            var start = stream.ReadInt64();
            var end = stream.ReadInt64();

            return new Span {Start = start, End = end};
        }

        public static string DefaultFile(string directory, string filename)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, filename);
        }
    }
}