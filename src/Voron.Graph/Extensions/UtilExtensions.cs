using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Voron.Util.Conversion;

namespace Voron.Graph
{
    public unsafe static class UtilExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Slice ToSlice(this long id)
        {		
			//TODO: investigate here how to create slice without allocations
			//perhaps cast to pointer somehow?
			return new Slice(EndianBitConverter.Big.GetBytes(id));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadToEnd(this Stream stream)
        {
            var data = new byte[stream.Length - stream.Position];
            stream.Read(data, 0, data.Length);
            return data;
        }
    }
}
