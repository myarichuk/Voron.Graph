using Newtonsoft.Json.Linq;
using System;

namespace Voron.Graph
{
    public class Edge : IEquatable<Edge>
    {

        public EdgeTreeKey Key { get; private set; }

        public JObject Data { get; private set; }

        public Etag Etag { get; internal set; }

        public short Weight { get; internal set; }

        public Edge(long nodeKeyFrom, long nodeKeyTo, JObject data, ushort type = 0, Etag etag = null,short weight = 1)           
        {
            Key = new EdgeTreeKey
            {
                NodeKeyFrom = nodeKeyFrom,
                NodeKeyTo = nodeKeyTo,
                Type = type,
            };

            Data = data;
            Etag = etag ?? Etag.Empty;
            Weight = weight;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Key.GetHashCode() * 397) ^ (Data.GetHashCode() * 397) ^ (Weight.GetHashCode() * 397) ^ Etag.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            if (ReferenceEquals(obj, null))
                return false;

            var otherEdge = obj as Edge;

            return Key.Equals(otherEdge.Key);
        }

        public bool Equals(Edge other)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            return Key.Equals(other.Key);
        }
    }
}
