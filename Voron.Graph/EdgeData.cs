using System.Runtime.InteropServices;

namespace Voron.Graph
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EdgeHeader
    {
        [FieldOffset(0)]
        public readonly long From;

        [FieldOffset(sizeof(long))] 
        public readonly long OutgoingEdgeCount;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct EdgeData
    {
        [FieldOffset(0)]
        public readonly long To;

        [FieldOffset(sizeof(long))] 
        public readonly long EdgeDataId;
    }
}
