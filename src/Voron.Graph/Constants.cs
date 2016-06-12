using Sparrow;
using System.Diagnostics;
using System.Reflection;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public static class Constants
	{
		public static class Indexes
		{
		
		}

		public static class SystemKeys
		{
			public const string GraphSystemDataPage = "SystemRawDataCreated";

			public const string NextVertexEtagEntry = "NextVertexEtagEntry";
			public const string NextIdEntry = "NextIdEntry";

			public const string NextEdgeEtagEntry = "NextEdgeEtagEntry";
			public const string NextEdgeIdEntry = "NextEdgeIdEntry";
		}

		public static class Schema
		{
			public const string SystemDataTree = "Voron.Graph.SystemDataTree";
			public const string Vertices = "Voron.Graph.VertexTree";
			public const string Edges = "Voron.Graph.AdjacencyList";
			public const string EtagToVertexTree = "Voron.Graph.EtagToVertexTree";
			public const string EtagToAdjacencyTree = "Voron.Graph.EtagToAdjacencyTree";
		}
	}
}
