using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Search;
using FluentAssertions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class SearchTests : BaseGraphTest
    {
        public System.Threading.CancellationTokenSource CancelTokenSource;

        [TestInitialize]
        public void InitTest()
        {
            CancelTokenSource = new System.Threading.CancellationTokenSource();
        }

        [TestMethod]
        public async Task BFS_FindOne_with_connected_root_should_return_correct_results()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node2;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

                graph.Commands.CreateEdgeBetween(tx, node3, node1);
                graph.Commands.CreateEdgeBetween(tx, node3, node2);

                graph.Commands.CreateEdgeBetween(tx, node2, node2);
                graph.Commands.CreateEdgeBetween(tx, node1, node3);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var bfs = new BreadthFirstSearch(graph, CancelTokenSource.Token);
                var node = await bfs.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

                node.Should().NotBeNull();
                node.Key.Should().Be(node2.Key);
            }
        }

        [TestMethod]
        public async Task BFS_FindOne_with_only_root_node_in_graph_returns_correct_results()
        {
            var graph = new GraphStorage("TestGraph", Env);
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var bfs = new BreadthFirstSearch(graph, CancelTokenSource.Token);
                var node = await bfs.FindOne(tx, data => ValueFromJson<string>(data).Equals("test1"));

                node.Should().NotBeNull();
            }
        }

        [TestMethod]
        public async Task BFS_FindOne_with_disconnected_root_should_return_null()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node2;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

                graph.Commands.CreateEdgeBetween(tx, node3, node1);
                graph.Commands.CreateEdgeBetween(tx, node3, node2);

                //note: root node is selected by using a first node that was added
                //since Voron.Graph is a directed graph - no node leads from the root node means nothing
                //can be found

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var bfs = new BreadthFirstSearch(graph, CancelTokenSource.Token);
                var node = await bfs.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

                node.Should().BeNull();
            }
        }

        [TestMethod]
        public async Task BFS_FindMany_with_connected_root_returns_correct_results()
        {
            var graph = new GraphStorage("TestGraph", Env);
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
                var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

                graph.Commands.CreateEdgeBetween(tx, node3, node1);
                graph.Commands.CreateEdgeBetween(tx, node3, node2);

                graph.Commands.CreateEdgeBetween(tx, node3, node4);
                graph.Commands.CreateEdgeBetween(tx, node2, node2);
                graph.Commands.CreateEdgeBetween(tx, node2, node4);
                graph.Commands.CreateEdgeBetween(tx, node1, node3);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var bfs = new BreadthFirstSearch(graph, CancelTokenSource.Token);
                var nodes = await bfs.FindMany(tx, data => ValueFromJson<string>(data).Contains("2") ||
                                                           ValueFromJson<string>(data).Contains("4"));

                nodes.Select(x => ValueFromJson<string>(x.Data)).Should().Contain("test2", "test4");
            }
        }
    
    }
}
