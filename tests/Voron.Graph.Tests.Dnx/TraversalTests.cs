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

				//test both dfs and bfs
				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .WithStrategy(Traversal.Strategy.Bfs)
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

					results = storage.Traverse()
										 .WithStrategy(Traversal.Strategy.Dfs)
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
		public void Traversal_without_limits_should_traverse_all_edges1()
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
		public void Traversal_without_limits_should_traverse_all_edges2()
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
		public void Traversal_without_limits_should_traverse_all_edges3()
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
						.Contain(new[] { id1, id2, id3, id4, id5 });
				}
			}
		}

		[Fact]
		public void Traversal_with_min_depth_should_traverse_only_relevant_edges1()
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
						.Contain(new[] { id2, id3, id4, id5 });

					results = storage.Traverse()
									 .WithMinDepth(2)
									 .Execute(id1);

					results.Should()
						.HaveCount(3)
						.And
						.Contain(new[] { id3, id4, id5 });

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
		public void Traversal_with_min_depth_should_traverse_relevant_edges1_with_DFS()
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
										 .WithStrategy(Traversal.Strategy.Dfs)
										 .WithMinDepth(1)
										 .Execute(id1);

					results.Should()
						.HaveCount(4)
						.And
						.Contain(new[] { id2, id3, id4, id5 });

					results = storage.Traverse()
									 .WithStrategy(Traversal.Strategy.Dfs)
									 .WithMinDepth(2)
									 .Execute(id1);

					results.Should()
						.HaveCount(3)
						.And
						.Contain(new[] { id3, id4, id5 });

					results = storage.Traverse()
										 //zero-based depth,
										 //4 levels in total
								     .WithStrategy(Traversal.Strategy.Dfs)
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
		public void Traversal_should_travel_loops_once1()
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

		[Fact]
		public void Traversal_should_travel_loops_once2()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 1, 1, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id3);
					storage.AddEdge(tx, id3, id1);
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

					results = storage.Traverse()
									 .Execute(id2);

					results.Should()
					.HaveCount(3)
					.And
					.ContainInOrder(id2, id3, id1);
				}
			}
		}

		[Fact]
		public void Traversal_with_max_results_lower_than_actual_results_should_have_no_effect()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 1, 1, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id3);
					storage.AddEdge(tx, id3, id1);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .WithMaxResults(50)
										 .Execute(id1);

					results.Should()
						.HaveCount(3)
						.And
						.ContainInOrder(id1, id2, id3);

					results = storage.Traverse()
									 .Execute(id2);

					results.Should()
					.HaveCount(3)
					.And
					.ContainInOrder(id2, id3, id1);
				}
			}
		}

		[Fact]
		public void Traversal_with_max_results_should_return_proper_number_of_results1()
		{
			using (var storage = new GraphStorage())
			{
				long id1, id2, id3;
				using (var tx = storage.WriteTransaction())
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					id3 = storage.AddVertex(tx, new byte[] { 1, 1, 1 });
					storage.AddEdge(tx, id1, id2);
					storage.AddEdge(tx, id2, id3);
					storage.AddEdge(tx, id3, id1);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
				{
					var results = storage.Traverse()
										 .WithMaxResults(2)
										 .Execute(id1);

					results.Should()
						.HaveCount(2)
						.And
						.ContainInOrder(id1, id2);

					results = storage.Traverse()
									 .WithMaxResults(2)
									 .Execute(id2);

					results.Should()
					.HaveCount(2)
					.And
					.ContainInOrder(id2, id3);
				}
			}
		}
	}
}
