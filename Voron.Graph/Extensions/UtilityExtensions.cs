using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Voron.Util.Conversion;

namespace Voron.Graph.Extensions
{
    public static class UtilityExtensions
    {
        private static readonly int EdgeTreeKeySize = Marshal.SizeOf(typeof(EdgeTreeKey));
        private static readonly int SizeOfUShort = Marshal.SizeOf(typeof(ushort));
        private static readonly int SizeOfShort = Marshal.SizeOf(typeof(short));
        private static readonly int SizeOfInt = Marshal.SizeOf(typeof(int));
        private static readonly int SizeOfLong = Marshal.SizeOf(typeof(long));

        internal static Stream ToStream(this JObject jsonObject)
        {
            if (jsonObject == null)
                return Stream.Null;

            var stream = new MemoryStream();
            var writer = new BsonWriter(stream);
            jsonObject.WriteTo(writer);

            stream.Position = 0;
            return stream;
        }

        internal static JObject ToJObject(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("cannot deserialize unreadable stream");

            stream.Seek(0, SeekOrigin.Begin);

            var reader = new BsonReader(stream);
            return JObject.Load(reader);
        }

        internal static Slice ToSlice(this long key)
        {
            var buffer = new byte[SizeOfLong]; //TODO: refactor this with BufferPool implementation
            EndianBitConverter.Big.CopyBytes(key, buffer, 0);
            return new Slice(buffer);
        }

        internal static Slice ToSlice(this int key)
        {
            var buffer = new byte[SizeOfInt]; //TODO: refactor this with BufferPool implementation
            EndianBitConverter.Big.CopyBytes(key, buffer, 0);
            return new Slice(buffer);
        }

        internal static Slice ToSlice(this EdgeTreeKey edgeKey)
        {
            var sliceWriter = new SliceWriter(EdgeTreeKeySize);

            sliceWriter.WriteBigEndian(edgeKey.NodeKeyFrom);
            sliceWriter.WriteBigEndian(edgeKey.NodeKeyTo);
            sliceWriter.WriteBigEndian(edgeKey.Type);

            return sliceWriter.CreateSlice();
        }

	    internal static long ToNodeKey(this Slice key)
	    {
		    var keyData = new byte[SizeOfLong];
		    key.CopyTo(keyData);

		    return EndianBitConverter.Big.ToInt64(keyData, 0);
	    }

        internal static EdgeTreeKey ToEdgeTreeKey(this Slice edgeKey)
        {
            var keyData = new byte[EdgeTreeKeySize];
            edgeKey.CopyTo(keyData);
            var edgeTreeKey = new EdgeTreeKey
            {
                NodeKeyFrom = EndianBitConverter.Big.ToInt64(keyData, 0),
                NodeKeyTo = EndianBitConverter.Big.ToInt64(keyData, SizeOfLong),
                Type = EndianBitConverter.Big.ToUInt16(keyData, SizeOfLong * 2)
            };

            return edgeTreeKey;
        }

        internal static Etag ToEtag(this Stream stream)
        {
            if (stream.CanRead == false)
                throw new ArgumentException("Cannot read from the stream --> unreadable!");

            if (stream.Length - stream.Position > Etag.Size)
                throw new ArgumentException("Invalid etag size in stream - data is corrupted?");

            var etagBytes = new byte[Etag.Size];
            stream.Read(etagBytes, 0, Etag.Size);

            var timestamp = EndianBitConverter.Big.ToInt64(etagBytes, 0);
            var count = EndianBitConverter.Big.ToInt64(etagBytes, SizeOfLong);

            return new Etag(count, timestamp);
        }

    }
}
