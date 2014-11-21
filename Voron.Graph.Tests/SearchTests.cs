using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using FluentAssertions;
using System.Threading;
using Voron.Graph.Extensions;
using System.Collections.Generic;

namespace Voron.Graph.Tests
{
    //TODO : write more tests
    [TestClass]
	public class SearchTests : BaseGraphTest, IDisposable
    {
        public CancellationTokenSource cancelTokenSource;

        private class NodeRecordingVisitor : IVisitor
        {
            public readonly List<long> DiscoveredNodeKeys;

            public NodeRecordingVisitor()
            {
                DiscoveredNodeKeys = new List<long>();
            }

            public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
            {
                DiscoveredNodeKeys.Add(traversalNodeInfo.CurrentNode.Key);
            }

            public void DiscoverAdjacent(Primitives.NodeWithEdge neighboorNode)
            {
            }


            public bool ShouldStopTraversal
            {
                get { return false; }
            }


            public bool ShouldSkipCurrentNode(TraversalNodeInfo traversalNodeInfo)
            {
                return false;
            }


            public bool ShouldSkipAdjacentNode(Primitives.NodeWithEdge adjacentNode)
            {
                return false;
            }
        }

        [TestInitialize]
        public void InitTest()
        {
            cancelTokenSource = new CancellationTokenSource();
        }       
        
        /*
         *  node1 -> node2 -> node5 -> node6    node7 -> node8
         *    |               ^                   |
         *    |               |                   L -> node9
         *    L -> node3 -> node4
         *    
         */
        [TestMethod]
        public void Search_with_multiple_disconnected_subgraph_should_only_traverse_reachable_nodes()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5, node6, node7, node8, node9;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(5));
                node6 = graph.Commands.CreateNode(tx, JsonFromValue(6));
                node7 = graph.Commands.CreateNode(tx, JsonFromValue(7));
                node8 = graph.Commands.CreateNode(tx, JsonFromValue(8));
                node9 = graph.Commands.CreateNode(tx, JsonFromValue(9));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node3.ConnectWith(tx, node4, graph);
                node2.ConnectWith(tx, node5, graph);
                node4.ConnectWith(tx, node5, graph);
                node5.ConnectWith(tx, node6, graph);

                node7.ConnectWith(tx, node8, graph);
                node7.ConnectWith(tx, node9, graph);

                tx.Commit();
            }

            var resultsBfs_subgraph1 = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, cancelTokenSource.Token);
            var resultsBfs_subgraph2 = graph.Find(node7, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, cancelTokenSource.Token);

            resultsBfs_subgraph1.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5 && x < 7)
                                                                    .And.HaveCount(2);
            resultsBfs_subgraph2.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5 && x >=7)
                                                                    .And.HaveCount(3);
        }

        /*
         *  node1 -> node2 -> node5 -> node6
         *    |               ^
         *    |               |
         *    L -> node3 -> node4
         *    
         */
        [TestMethod]
        public void Search_with_result_limit_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5, node6;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(5));
                node6 = graph.Commands.CreateNode(tx, JsonFromValue(6));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node3.ConnectWith(tx, node4, graph);
                node2.ConnectWith(tx, node5, graph);
                node4.ConnectWith(tx, node5, graph);
                node5.ConnectWith(tx, node6, graph);

                tx.Commit();
            }

            var results = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, cancelTokenSource.Token,1);
            results.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x == 5)
                                                                    .And.HaveCount(1);
        }

        
        /*
         *  node1 -> node2 -> node5 -> node6
         *    |               ^
         *    |               |
         *    L -> node3 -> node4
         *    
         */
        //implicitly this test also verifies that traversal algorithm correctly colors visited nodes
        [TestMethod]
        public void Search_with_multiple_graph_path_should_return_unique_results()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5, node6;
            using(var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(5));
                node6 = graph.Commands.CreateNode(tx, JsonFromValue(6));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node3.ConnectWith(tx, node4, graph);
                node2.ConnectWith(tx, node5, graph);
                node4.ConnectWith(tx, node5, graph);
                node5.ConnectWith(tx, node6, graph);

                tx.Commit();
            }

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, cancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.DFS, cancelTokenSource.Token);

            resultsBfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5)
                                                                    .And.HaveCount(2);
            resultsDfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5)
                                                                    .And.HaveCount(2);
        }

        [TestMethod]
        public async Task SearchAsync_with_multiple_graph_path_should_return_unique_results()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5, node6;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(5));
                node6 = graph.Commands.CreateNode(tx, JsonFromValue(6));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node3.ConnectWith(tx, node4, graph);
                node2.ConnectWith(tx, node5, graph);
                node4.ConnectWith(tx, node5, graph);
                node5.ConnectWith(tx, node6, graph);

                tx.Commit();
            }

            var resultsBfs = await graph.FindAsync(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, cancelTokenSource.Token);
            var resultsDfs = await graph.FindAsync(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.DFS, cancelTokenSource.Token);

            resultsBfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5)
                                                                    .And.HaveCount(2);
            resultsDfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 5)
                                                                    .And.HaveCount(2);
        }

        /* 
         *  node1 -> node2
         *   |
         *   L-> node3 -> node4
         * 
         */  

        [TestMethod]
        public void Simple_search_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4;
            using(var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node3.ConnectWith(tx, node4, graph);

                tx.Commit();
            }

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 3, TraversalType.BFS, cancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 3, TraversalType.DFS, cancelTokenSource.Token);

            resultsBfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 3)
                                                                    .And.HaveCount(2);
            resultsDfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x >= 3)
                                                                    .And.HaveCount(2);
        }

        [TestMethod]
        public void Search_for_when_one_node_in_graph_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));

                tx.Commit();
            }

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) == 1, TraversalType.BFS, cancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) == 1, TraversalType.DFS, cancelTokenSource.Token);
            resultsBfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x == 1)
                                                                    .And.HaveCount(1);
            resultsDfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x == 1)
                                                                    .And.HaveCount(1);
        }
		public void Dispose()
		{
			if (cancelTokenSource != null)
			{
				cancelTokenSource.Dispose();
				cancelTokenSource = null;
			}
		}
	}
}