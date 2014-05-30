using Newtonsoft.Json.Linq;

namespace Voron.Graph
{
    public class Edge
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
    }
}
