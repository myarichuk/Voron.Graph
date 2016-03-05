using System.Diagnostics;
using Voron.Data.RawData;

namespace Voron.Graph
{
    public unsafe static class ActiveRawDataSmallSectionExtensions
    {
        public static bool TryWriteInt64(this ActiveRawDataSmallSection dataSection, long id, long value)
        {
            return dataSection.TryWrite(id, (byte*)(&value), sizeof(long));
        }

        public static bool TryWriteInt32(this ActiveRawDataSmallSection dataSection, long id, int value)
        {
            return dataSection.TryWrite(id, (byte*)(&value), sizeof(int));
        }

        public static long ReadInt64(this ActiveRawDataSmallSection dataSection, long id)
        {
            int size;
            var dataPtr = dataSection.DirectRead(id, out size);
            Debug.Assert(size == sizeof(long));

            return *(long*)dataPtr;
        }

        public static int ReadInt32(this ActiveRawDataSmallSection dataSection, long id)
        {
            int size;
            var dataPtr = dataSection.DirectRead(id, out size);
            Debug.Assert(size == sizeof(int));

            return *(int*)dataPtr;
        }
    }
}
