namespace Voron.Graph
{
	public static class Constants
	{
		public const string MetadataTree = "AdminTree";

		public const string NextIdKey = "NextIdKey";

		public static class SystemKeys
		{
			public const string GraphSystemDataPage = "SystemRawDataCreated";

			public const string NextVertexIdEntry = "NextVertexIdEntry";
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
