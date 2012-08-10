using System;
using System.IO;

namespace bsharptree.example.simpledb
{
    public class Record
    {
        public RecordStatus Status;
        public byte[] Value;

        public byte[] ToBytes()
        {
            var databytes = new byte[1 + 4 + Value.Length];
            databytes[0] = (byte) Status;
            //Buffer.BlockCopy(BitConverter.GetBytes(Hits), 0, databytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(Value.Length), 0, databytes, 1, 4);
            Buffer.BlockCopy(Value, 0, databytes, 5, Value.Length);

            return databytes;
        }

        public static Record FromStream(Stream dataStream)
        {
            var results = new Record();

            var header = new byte[5];
            dataStream.Read(header, 0, header.Length);

            results.Status = (RecordStatus)header[0];
            var valueLength = BitConverter.ToInt32(header, 1);

            results.Value = new byte[valueLength];
            dataStream.Read(results.Value, 0, results.Value.Length);

            return results;
        }
    }
}