using System.Runtime.InteropServices;

namespace Voron.Graph
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EdgeTreeKey
    {
        public long NodeKeyFrom { get; set; }

        public long NodeKeyTo { get; set; }

        //does not have a specific meaning, this field can be ignored or used as kind of metadata
        public ushort Type { get; set; }

        public short Weight { get; set; }
    }
}
