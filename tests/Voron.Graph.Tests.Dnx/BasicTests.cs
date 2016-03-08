using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Voron.Graph.Tests
{
    public class BasicTests
    {
        [Fact]
        public void Initialization_should_work()
        {
            //create in-memory 
            using (var storage = new GraphStorage())
            { }

            //create persisted
            var tempPath = Path.GetTempPath() + Path.DirectorySeparatorChar + Guid.NewGuid();
            try
            {
                using (var storage = new GraphStorage(tempPath))
                {
                }
            }
            finally
            {
                Directory.Delete(tempPath,true);
            }
        }

        [Fact]
        public void Simple_vertex_read_write_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long id1,id2;
                using (var tx = storage.WriteTransaction())
                {
                    id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
                    id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
                    tx.Commit();
                }

                using (var tx = storage.ReadTransaction())
                {
                    var data1 = storage.ReadVertexData(tx, id1);
                    Assert.NotNull(data1);
                    Assert.Equal(new byte[] { 1, 2, 3 }, data1);
                    var data2 = storage.ReadVertexData(tx, id2);
                    Assert.NotNull(data2);
                    Assert.Equal(new byte[] { 3, 2, 1 }, data2);
                }
            }
        }

        [Fact]
        public void Simple_vertex_delete_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long id;
                using (var tx = storage.WriteTransaction())
                {
                    id = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
                    tx.Commit();
                }

                using (var tx = storage.WriteTransaction())
                {
                    storage.RemoveVertex(tx, id);
                    tx.Commit();
                }

                using (var tx = storage.ReadTransaction())
                    Assert.Null(storage.ReadVertexData(tx, id));
            }
        }

        [Fact]
        public void Simple_edge_read_write_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long vertexId1, vertexId2, edgeId;
                using (var tx = storage.WriteTransaction())
                {
                    vertexId1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
                    vertexId2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });

                    edgeId = storage.AddEdge(tx, vertexId1, vertexId2, new byte[] { 5, 6, 7, 8 });

                    tx.Commit();
                }

                using (var tx = storage.ReadTransaction())
                {
                    var data = storage.ReadEdgeData(tx, edgeId);
                    Assert.Equal(new byte[] { 5, 6, 7, 8 }, data);
                }
            }
        }

		[Fact]
		public void Simple_edge_delete_should_work()
		{
			using (var storage = new GraphStorage())
			{
				long vertexId1, vertexId2, edgeId;
				using (var tx = storage.WriteTransaction())
				{
					vertexId1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					vertexId2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });

					edgeId = storage.AddEdge(tx, vertexId1, vertexId2, new byte[] { 5, 6, 7, 8 });

					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var data = storage.ReadEdgeData(tx, edgeId);
					Assert.NotNull(data);
				}

				using (var tx = storage.WriteTransaction())
				{
					storage.RemoveEdge(tx, edgeId);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var data = storage.ReadEdgeData(tx, edgeId);
					Assert.Null(data);
				}
			}
		}

		[Fact]
        public unsafe void Simple_edge_read_write_without_data_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long vertexId1, vertexId2, edgeId;
                using (var tx = storage.WriteTransaction())
                {
                    vertexId1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
                    vertexId2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });

                    edgeId = storage.AddEdge(tx, vertexId1, vertexId2);

                    tx.Commit();
                }

                using (var tx = storage.ReadTransaction())
                {
                    Assert.Empty(storage.ReadEdgeData(tx, edgeId));

                    int size;
                    var data = storage.ReadEdgeData(tx, edgeId, out size);
                    Assert.Equal(0, size);

                    //will return valid ptr, but the size of data chunk is zero
                    Assert.False(null == data);
                }
            }
        }    

        [Fact]
        public unsafe void GetAdjacent_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long vertexId1, vertexId2, vertexId3, vertexId4, vertexId5;
                using (var tx = storage.WriteTransaction())
                {
                    vertexId1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
                    vertexId2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
                    vertexId3 = storage.AddVertex(tx, new byte[] { 4,5,6 });
                    vertexId4 = storage.AddVertex(tx, new byte[] { 7 });
                    vertexId5 = storage.AddVertex(tx, new byte[] { 8 });

                    storage.AddEdge(tx, vertexId1, vertexId2);
                    storage.AddEdge(tx, vertexId1, vertexId3);
                    storage.AddEdge(tx, vertexId1, vertexId5);

					storage.AddEdge(tx, vertexId4, vertexId2);
					storage.AddEdge(tx, vertexId4, vertexId4);
					storage.AddEdge(tx, vertexId4, vertexId1);

					storage.AddEdge(tx, vertexId2, vertexId3);
					tx.Commit();
                }

                using (var tx = storage.ReadTransaction())
                {
                    var adjacentVertices = storage.GetAdjacent(tx, vertexId1).ToList();
                    adjacentVertices.Should()
						.HaveCount(3)
						.And.Contain(new[] 
                            {
                                vertexId2,
                                vertexId3,
                                vertexId5
                            });

					adjacentVertices = storage.GetAdjacent(tx, vertexId2).ToList();
					adjacentVertices.Should()
						.HaveCount(1)
						.And.Contain(new[]
							{
								vertexId3
							});

					adjacentVertices = storage.GetAdjacent(tx, vertexId4).ToList();
					adjacentVertices.Should()
						.HaveCount(3)
						.And.Contain(new[]
							{
								vertexId1,
								vertexId2,
								vertexId4
							});

					
				}
            }
        }
    }
}
