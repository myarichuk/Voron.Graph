using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Search;
using FluentAssertions;
using System.Threading;
using Voron.Graph.Extensions;

namespace Voron.Graph.Tests
{
    //TODO : write more tests
    [TestClass]
    public class SearchTests : BaseGraphTest
    {
        public CancellationTokenSource CancelTokenSource;

        [TestInitialize]
        public void InitTest()
        {
            CancelTokenSource = new CancellationTokenSource();
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

            var results = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, CancelTokenSource.Token,1);
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

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.BFS, CancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 5, TraversalType.DFS, CancelTokenSource.Token);

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

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 3, TraversalType.BFS, CancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) >= 3, TraversalType.DFS, CancelTokenSource.Token);

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

            var resultsBfs = graph.Find(node1, data => ValueFromJson<int>(data) == 1, TraversalType.BFS, CancelTokenSource.Token);
            var resultsDfs = graph.Find(node1, data => ValueFromJson<int>(data) == 1, TraversalType.DFS, CancelTokenSource.Token);
            resultsBfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x == 1)
                                                                    .And.HaveCount(1);
            resultsDfs.Select(x => ValueFromJson<int>(x.Data)).Should().OnlyContain(x => x == 1)
                                                                    .And.HaveCount(1);
        }
    }
}