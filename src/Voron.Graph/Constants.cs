using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public static class Constants
	{
		public static class Indexes
		{
			public static class VertexTable
			{
				public static readonly TableSchema.SchemaIndexDef Etag = new TableSchema.SchemaIndexDef
				{
					Name = "VertexEtag",
					NameAsSlice = "VertexEtag",
					StartIndex = (int)VertexTableFields.Etag,
					IsGlobal = true
				};				
			}

			public static class EdgeTable
			{
				public static readonly TableSchema.SchemaIndexDef Etag = new TableSchema.SchemaIndexDef
				{
					Name = "EdgeEtag",
					NameAsSlice = "EdgeEtag",
					StartIndex = (int)EdgeTableFields.Etag,
					IsGlobal = true
				};
				
				public static readonly TableSchema.SchemaIndexDef FromToIndex = new TableSchema.SchemaIndexDef
				{
					Name = "FromToIndex",
					NameAsSlice = "FromToIndex",
					StartIndex = (int)EdgeTableFields.FromKey,		
					Count = 2,			
					IsGlobal = true
				};

				public static readonly TableSchema.FixedSizeSchemaIndexDef ToIdIndex = new TableSchema.FixedSizeSchemaIndexDef
				{
					Name = "EdgeToId",
					NameAsSlice = "EdgeToId",
					StartIndex = (int)EdgeTableFields.ToKey,
					IsGlobal = true
				};
			}
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
