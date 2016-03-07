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
				public static readonly TableSchema.SchemaIndexDef Key = new TableSchema.SchemaIndexDef
				{
					Name = "VertexId",
					NameAsSlice = "VertexId",
					StartIndex = (int)VertexTableFields.Id,
					IsGlobal = true
				};

				public static readonly TableSchema.FixedSizeSchemaIndexDef Etag = new TableSchema.FixedSizeSchemaIndexDef
				{
					Name = "VertexEtag",
					NameAsSlice = "VertexEtag",
					StartIndex = (int)VertexTableFields.Etag,
					IsGlobal = true
				};
			}

			public static class EdgeTable
			{
				public static readonly TableSchema.SchemaIndexDef Key = new TableSchema.SchemaIndexDef
				{
					Name = "EdgeId",
					NameAsSlice = "EdgeId",
					StartIndex = (int)EdgeTableFields.Id,
					IsGlobal = true
				};

				public static readonly TableSchema.FixedSizeSchemaIndexDef EtagIndex = new TableSchema.FixedSizeSchemaIndexDef
				{
					StartIndex = (int)EdgeTableFields.Etag,
					Name = "EdgeEtag",
					NameAsSlice = "EdgeEtag",
					IsGlobal = true
				};

				public static readonly TableSchema.SchemaIndexDef FromToIndex = new TableSchema.SchemaIndexDef
				{
					Name = "FromToIndex",
					NameAsSlice = "FromToIndex",
					StartIndex = (int)EdgeTableFields.FromKey,		
					MultiValue = true,
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
