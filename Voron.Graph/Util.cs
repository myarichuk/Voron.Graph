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
        private static int EdgeTreeKeySize = Marshal.SizeOf(typeof(EdgeTreeKey));
        private static int SizeOfUShort = Marshal.SizeOf(typeof(ushort));
        private static int SizeOfLong = Marshal.SizeOf(typeof(long));

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
            var sizeofLong = sizeof(long);
            var prefixBytes = new byte[sizeofLong * 2]; //TODO : this should be taken from Buffer Pool
            BigEndianBitConverter.Big.CopyBytes(nodeFrom.Key, prefixBytes, 0);
            BigEndianBitConverter.Big.CopyBytes(nodeTo.Key, prefixBytes, sizeofLong);
            return new Slice(prefixBytes);
        }

        internal static Slice ToSlice(this long key)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(long))]; //TODO: refactor this with BufferPool implementation
            BigEndianBitConverter.Big.CopyBytes(key, buffer, 0);
            return new Slice(buffer);
        }

        internal static Slice ToSlice(this int key)
        {
            var buffer = new byte[Marshal.SizeOf(key)]; //TODO: refactor this with BufferPool implementation
            BigEndianBitConverter.Big.CopyBytes(key, buffer, 0);
            return new Slice(buffer);
        }

        internal static Slice ToSlice(this EdgeTreeKey edgeKey)
        {
            var keyData = new byte[EdgeTreeKeySize];

            BigEndianBitConverter.Big.CopyBytes(edgeKey.NodeKeyFrom, keyData, 0);
            BigEndianBitConverter.Big.CopyBytes(edgeKey.NodeKeyTo, keyData, SizeOfLong);
            BigEndianBitConverter.Big.CopyBytes(edgeKey.Type, keyData, SizeOfLong + SizeOfUShort);

            return new Slice(keyData);
        }


        internal static EdgeTreeKey ToEdgeTreeKey(this Slice edgeKey)
        {
            var keyData = new byte[EdgeTreeKeySize];
            edgeKey.CopyTo(keyData);
            var edgeTreeKey = new EdgeTreeKey
            {
                NodeKeyFrom = BigEndianBitConverter.Big.ToInt64(keyData, 0),
                NodeKeyTo = BigEndianBitConverter.Big.ToInt64(keyData, SizeOfLong),
                Type = BigEndianBitConverter.Big.ToUInt16(keyData, SizeOfLong + SizeOfUShort)
            };
            
            return edgeTreeKey;
        }    
    }
}
