using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voron.Util.Conversion;

namespace Voron.Graph
{


    internal unsafe static class Util
    {
	    private static readonly int EdgeTreeKeySize = Marshal.SizeOf(typeof(EdgeTreeKey));
		private static readonly int SizeOfUShort = Marshal.SizeOf(typeof(ushort));
		private static readonly int SizeOfInt = Marshal.SizeOf(typeof(int));
		private static readonly int SizeOfLong = Marshal.SizeOf(typeof(long));

        internal static Slice EdgeKeyPrefix(Node nodeFrom, Node nodeTo)
        {
			var prefixBytes = new byte[SizeOfLong * 2]; //TODO : this should be taken from Buffer Pool
            EndianBitConverter.Big.CopyBytes(nodeFrom.Key, prefixBytes, 0);
			EndianBitConverter.Big.CopyBytes(nodeTo.Key, prefixBytes, SizeOfLong);
            return new Slice(prefixBytes);
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

        internal static void EtagAndValueFromStream(Stream source, out Etag etag, out JObject value)
        {
            source.Position = 0;
            var etagBytes = new byte[Etag.Size];
            var reader = new BsonReader(source);

            source.Read(etagBytes, 0, Etag.Size);
            etag = new Etag(etagBytes);
            value = JObject.Load(reader);
        }

    }
}
