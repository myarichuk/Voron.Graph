using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public class Edge
    {

        public EdgeTreeKey Key { get; private set; }

        public JObject Data { get; private set; }

        public Edge(EdgeTreeKey key, JObject data, ushort type = 0)
        {
            Key = new EdgeTreeKey
            {
                NodeKeyFrom = key.NodeKeyFrom,
                NodeKeyTo = key.NodeKeyTo,
                Type = type
            };

            Data = data;
        }

        public Edge(long nodeKeyFrom, long nodeKeyTo, JObject data, ushort type = 0)
            : this(new EdgeTreeKey
            {
                NodeKeyFrom = nodeKeyFrom,
                NodeKeyTo = nodeKeyTo,
                Type = type
            },data)
        {

        }
    }
}
