using Newtonsoft.Json.Linq;

namespace Voron.Graph
{
    public class Edge
    {

        public EdgeTreeKey Key { get; private set; }

        public JObject Data { get; private set; }

        public Etag Etag { get; internal set; }

        public Edge(EdgeTreeKey key, JObject data, Etag etag = null)
        {
            Key = new EdgeTreeKey
            {
                NodeKeyFrom = key.NodeKeyFrom,
                NodeKeyTo = key.NodeKeyTo,
                Type = key.Type
            };

            Data = data;
            Etag = etag ?? Etag.Empty;
        }

        public Edge(long nodeKeyFrom, long nodeKeyTo, JObject data, ushort type = 0, Etag etag = null)
            : this(new EdgeTreeKey
            {
                NodeKeyFrom = nodeKeyFrom,
                NodeKeyTo = nodeKeyTo,
                Type = type
            }, data, etag)
        {

        }
    }
}
