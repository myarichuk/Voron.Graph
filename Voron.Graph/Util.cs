using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    internal static class Util
    {
        internal static string CreateEdgeTreeKey(string nodeKeyFrom, string nodeKeyTo)
        {
            return String.Format("{0}|{1}", nodeKeyFrom, nodeKeyTo);
        }

        internal static EdgeTreeKey ParseEdgeTreeKey(string key)
        {
            var keyParts = key.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyParts.Length != 2)
                throw new ArgumentException("Invalid Edge Tree key format, could not parse.");

            return new EdgeTreeKey
            {
                NodeKeyFrom = keyParts[0],
                NodeKeyTo = keyParts[1]
            };
        }

        internal class EdgeTreeKey
        {
            public string NodeKeyFrom { get; set; }

            public string NodeKeyTo { get; set; }
        }
    }
}
