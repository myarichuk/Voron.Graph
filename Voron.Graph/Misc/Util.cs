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

        internal static Slice EdgeKeyPrefix(Node nodeFrom, Node nodeTo)
        {
			var prefixBytes = new byte[SizeOfLong * 2]; //TODO : this should be taken from Buffer Pool
            EndianBitConverter.Big.CopyBytes(nodeFrom.Key, prefixBytes, 0);
			EndianBitConverter.Big.CopyBytes(nodeTo.Key, prefixBytes, SizeOfLong);
            return new Slice(prefixBytes);
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
            var keyData = new byte[EdgeTreeKeySize];

            EndianBitConverter.Big.CopyBytes(edgeKey.NodeKeyFrom, keyData, 0);
            EndianBitConverter.Big.CopyBytes(edgeKey.NodeKeyTo, keyData, SizeOfLong);
            EndianBitConverter.Big.CopyBytes(edgeKey.Type, keyData, SizeOfLong + SizeOfUShort);

            return new Slice(keyData);
        }


        internal static EdgeTreeKey ToEdgeTreeKey(this Slice edgeKey)
        {
            var keyData = new byte[EdgeTreeKeySize];
            edgeKey.CopyTo(keyData);
            var edgeTreeKey = new EdgeTreeKey
            {
                NodeKeyFrom = EndianBitConverter.Big.ToInt64(keyData, 0),
                NodeKeyTo = EndianBitConverter.Big.ToInt64(keyData, SizeOfLong),
                Type = EndianBitConverter.Big.ToUInt16(keyData, SizeOfLong + SizeOfUShort)
            };
            
            return edgeTreeKey;
        }

	    internal static Etag ToEtag(this Stream stream)
	    {
		    if(stream.CanRead == false)
				throw new ArgumentException("Cannot read from the stream --> unreadable!");

		    var sizeOfEtag = SizeOfLong*2;
		    if(stream.Length - stream.Position > sizeOfEtag)
				throw new ArgumentException("Invalid etag size in stream - data is corrupted?");

		    var etagBytes = new byte[sizeOfEtag];
		    stream.Read(etagBytes, 0, sizeOfEtag);

			var timestamp = EndianBitConverter.Big.ToInt64(etagBytes, 0);
			var count = EndianBitConverter.Big.ToInt64(etagBytes, SizeOfLong);

			return new Etag(count,timestamp);
		}
    }
}
