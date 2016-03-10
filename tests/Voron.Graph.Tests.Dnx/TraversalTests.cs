using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Voron.Graph.Tests.Dnx
{
    public class TraversalTests
    {
		[Fact]
		public void Traversal_without_edges_should_return_first_vertex()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .Execute(id1);

					results.Should()
						.HaveCount(1)
						.And
						.OnlyContain(r => r == id1);

					results = storage.Traverse()
								     .Execute(id2);

					results.Should()
						.HaveCount(1)
						.And
						.OnlyContain(r => r == id2);
				}
			}
		}

		[Fact]
		public void Traversal_without_limits_should_return_all_edges1()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					storage.AddEdge(tx, id1, id2);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .Execute(id1);

					results.Should()
						.HaveCount(2)
						.And
						.Contain(id1).And.Contain(id2);

					results = storage.Traverse()
									 .Execute(id2);

					results.Should()
						.HaveCount(1)
						.And
						.OnlyContain(r => r == id2);
				}
			}
		}

		[Fact]
		public void Traversal_without_limits_should_return_all_edges2()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id1, id3);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .Execute(id1);

					results.Should()
						.HaveCount(3)
						.And
						.ContainInOrder(id1, id2, id3);			
				}
			}
		}

		[Fact]
		public void Traversal_without_limits_should_return_all_edges3()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3,id4,id5;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id4 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id5 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id3);
					storage.AddEdge(tx, id2, id4);
					storage.AddEdge(tx, id4, id5);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .Execute(id1);

					results.Should()
						.HaveCount(5)
						.And
						.ContainInOrder(id1, id2, id3,id4,id5);
				}
			}
		}

		[Fact]
		public void Traversal_with_min_depth_should_return_proper_edges1()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3, id4, id5;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id4 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id5 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id3);
					storage.AddEdge(tx, id2, id4);
					storage.AddEdge(tx, id4, id5);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .WithMinDepth(1)
										 .Execute(id1);

					results.Should()
						.HaveCount(4)
						.And
						.ContainInOrder(id2, id3, id4, id5);

					results = storage.Traverse()
									 .WithMinDepth(2)
									 .Execute(id1);

					results.Should()
						.HaveCount(3)
						.And
						.ContainInOrder(id3, id4, id5);

					results = storage.Traverse()
									 //zero-based depth,
									 //4 levels in total
									 .WithMinDepth(3)
									 .Execute(id1);

					results.Should()
						.HaveCount(1)
						.And
						.Contain(id5);
				}
			}
		}		

		[Fact]
		public void Traversal_would_travel_loops_once()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id1);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .Execute(id1);

					results.Should()
						.HaveCount(2)
						.And
						.ContainInOrder(id1, id2);

					results = storage.Traverse()
									 .Execute(id2);

					results.Should()
					.HaveCount(2)
					.And
					.ContainInOrder(id2, id1);
				}
			}
		}
	}
}
