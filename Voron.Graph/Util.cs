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

        internal static byte* ToPtr(this EdgeTreeKey edgeKey)
        {
            return (byte*)&edgeKey;
        }

        internal static EdgeTreeKey ToEdgeTreeKey(this Slice edgeKey)
        {
            var keyData = new byte[Marshal.SizeOf(typeof(EdgeTreeKey))];
            edgeKey.CopyTo(keyData);
            fixed (byte* keyDataPtr = keyData)
                return *((EdgeTreeKey*)keyDataPtr);
        }    
    }
}
