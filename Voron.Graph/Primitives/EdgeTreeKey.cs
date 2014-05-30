using System.Runtime.InteropServices;
using System;

namespace Voron.Graph
{
    [StructLayout(LayoutKind.Sequential)]
    public class EdgeTreeKey : IEquatable<EdgeTreeKey>
    {
        public long NodeKeyFrom { get; set; }

        public long NodeKeyTo { get; set; }

        //does not have a specific meaning, this field can be ignored or used as kind of metadata
        public ushort Type { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(obj, this)) return true;

            var otherEdgeTreeKey = obj as EdgeTreeKey;
            return NodeKeyFrom == otherEdgeTreeKey.NodeKeyFrom &&
                   NodeKeyTo == otherEdgeTreeKey.NodeKeyTo &&
                   Type == otherEdgeTreeKey.Type;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (NodeKeyFrom.GetHashCode() * 397) ^ (NodeKeyTo.GetHashCode() * 397) ^ Type.GetHashCode();
            }           
        }

        public bool Equals(EdgeTreeKey other)
        {
            return Equals((object)other);
        }
    }
}
