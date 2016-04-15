using Sparrow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
    public unsafe static class TableValueReaderExtensions
    {
		public static byte[] ReadData(this TableValueReader reader, int index)
		{
			int size;
			var rawData = reader.Read(index, out size);
			var data = new byte[size];
			fixed (byte* dataPtr = data)
				Memory.Copy(dataPtr, rawData, size);

			return data;
		}
	}
}
