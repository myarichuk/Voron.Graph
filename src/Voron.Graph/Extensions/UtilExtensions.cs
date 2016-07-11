using Sparrow;
using System.IO;
using System.Runtime.CompilerServices;

namespace Voron.Graph
{
	public unsafe static class UtilExtensions
	{
		private static readonly SliceWriter _longWriter = new SliceWriter(sizeof(long)); 
		private static readonly object _writerSync = new object();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]				
		public static Slice ToSlice(this long val, ByteStringContext context)
		{
			lock (_writerSync) //precaution, should be uncontested
			{
				_longWriter.Reset();
				_longWriter.WriteBigEndian(val);
				return _longWriter.CreateSlice(context);
			}
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
