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
    [StructLayout(LayoutKind.Sequential)]
    public struct EdgeTreeKey
    {
        public long NodeKeyFrom { get; set; }

        public long NodeKeyTo { get; set; }

        public long EdgeWeight { get; set; }
    }

    internal unsafe static class Util
    {
        private static ushort EdgeTreeKeySize = (ushort)Marshal.SizeOf(typeof(EdgeTreeKey));
        private static int SizeOfLong = Marshal.SizeOf(typeof(long));

        internal static Slice ToSlice(this long key)
        {
            var buffer = new byte[Marshal.SizeOf(key)]; //TODO: refactor this with BufferPool implementation
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
            BigEndianBitConverter.Big.CopyBytes(edgeKey.EdgeWeight, keyData, SizeOfLong * 2);

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
                EdgeWeight = BigEndianBitConverter.Big.ToInt64(keyData, SizeOfLong * 2)
            };
            
            return edgeTreeKey;
        }    
    }
}
