using Newtonsoft.Json.Linq;
using System;

namespace Voron.Graph
{
    public class Node : IEquatable<Node>
    {
        public long Key { get; set; }

        public JObject Data { get; private set; }

        public Etag Etag { get; internal set; }

        public Node(long key, JObject data, Etag etag = null)
        {
            Key = key;
            Data = data;
            Etag = etag ?? Etag.Empty;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Node);
        }

        public bool Equals(Node other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(other, this))
                return true;

            return Key == other.Key;
        }
    }
}
