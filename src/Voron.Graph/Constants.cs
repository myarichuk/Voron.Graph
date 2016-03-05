using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public static class Constants
    {
        public static class SystemKeys
        {
            public const string GraphSystemDataPage = "SystemRawDataCreated";
            public const string LastEtagEntry = "LastEtagEntry";
            public const string NextVertexIdEntry = "NextVertexIdEntry";
        }

        public static class Schema
        {
            public const string SystemDataTree = "Voron.Graph.SystemDataTree";
            public const string VertexTree = "Voron.Graph.VertexTree";
            public const string AdjacencyList = "Voron.Graph.AdjacencyList";
            public const string EtagToVertexTree = "Voron.Graph.EtagToVertexTree";
            public const string EtagToAdjacencyTree = "Voron.Graph.EtagToAdjacencyTree";
        }
    }
}
