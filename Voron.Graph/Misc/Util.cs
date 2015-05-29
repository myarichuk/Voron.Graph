using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Voron.Graph.Misc;
using Voron.Util.Conversion;

namespace Voron.Graph
{
    internal static class Util
    {
	    private static readonly int EdgeTreeKeySize = Marshal.SizeOf(typeof(EdgeTreeKey));
		private static readonly int SizeOfUShort = Marshal.SizeOf(typeof(ushort));
		private static readonly int SizeOfInt = Marshal.SizeOf(typeof(int));
		private static readonly int SizeOfLong = Marshal.SizeOf(typeof(long));

        internal static Slice EdgeKeyPrefix(Node nodeFrom, Node nodeTo)
        {
			var prefixBytes = BufferPool.TakeBuffer(SizeOfLong * 2);
			try
			{
				EndianBitConverter.Big.CopyBytes(nodeFrom.Key, prefixBytes, 0);
				EndianBitConverter.Big.CopyBytes(nodeTo.Key, prefixBytes, SizeOfLong);
				return new Slice(prefixBytes);
			}
			finally
			{
				BufferPool.ReturnBuffer(prefixBytes);
			}
        }

		internal static Stream EtagWeightAndValueToStream(Etag etag, JObject value, short weight)
		{
			var stream = new MemoryStream();
			var etagBytes = etag.ToBytes();

			var weightBytes = BufferPool.TakeBuffer(2);
			try
			{
				EndianBitConverter.Big.CopyBytes(weight, weightBytes, 0);

				stream.Write(etagBytes, 0, etagBytes.Length);
				stream.Write(weightBytes, 0, 2);
			}
			finally
			{
				BufferPool.ReturnBuffer(weightBytes);
			}
			var writer = new BsonWriter(stream);
			value.WriteTo(writer);

			stream.Position = 0;
			return stream;
		}

        internal static Stream EtagAndValueToStream(Etag etag, JObject value)
        {
            var stream = new MemoryStream();
            var etagBytes = etag.ToBytes();

            stream.Write(etagBytes, 0, etagBytes.Length);
            
            var writer = new BsonWriter(stream);
            value.WriteTo(writer);

            stream.Position = 0;
            return stream;
        }

        internal static void EtagWeightAndValueFromStream(Stream source, out Etag etag, out short weight, out JObject value)
        {
            source.Position = 0;
            var etagBytes = BufferPool.TakeBuffer(Etag.Size);
			var weightBytes = BufferPool.TakeBuffer(2);

			try
			{
				var reader = new BsonReader(source);

				source.Read(etagBytes, 0, Etag.Size);
				source.Read(weightBytes, 0, 2);
				etag = new Etag(etagBytes);

	            weight = EndianBitConverter.Big.ToInt16(weightBytes, 0);
		        value = JObject.Load(reader);
			}
			finally
			{
				BufferPool.ReturnBuffer(etagBytes);
				BufferPool.ReturnBuffer(weightBytes);
			}

		}

		internal static void EtagAndValueFromStream(Stream source, out Etag etag, out JObject value)
        {
            source.Position = 0;
			var etagBytes = BufferPool.TakeBuffer(Etag.Size);
			var reader = new BsonReader(source);

			try
			{
				source.Read(etagBytes, 0, Etag.Size);
				etag = new Etag(etagBytes);
			}
			finally
			{
				BufferPool.ReturnBuffer(etagBytes);
			}
            value = JObject.Load(reader);
        }

    }
}
