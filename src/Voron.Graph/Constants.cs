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

			public const string NextVertexEtagEntry = "NextVertexEtagEntry";
			public const string NextVertexIdEntry = "NextVertexIdEntry";

			public const string NextEdgeEtagEntry = "NextEdgeEtagEntry";
			public const string NextEdgeIdEntry = "NextEdgeIdEntry";
		}

		public static class Schema
        {
            public const string SystemDataTree = "Voron.Graph.SystemDataTree";
            public const string Vertices = "Voron.Graph.VertexTree";
            public const string AdjacencyList = "Voron.Graph.AdjacencyList";
            public const string EtagToVertexTree = "Voron.Graph.EtagToVertexTree";
            public const string EtagToAdjacencyTree = "Voron.Graph.EtagToAdjacencyTree";
        }
    }
}
