using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.ShortestPath;
using Voron.Graph.Extensions;
using FluentAssertions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class DijkstraTests : BaseGraphTest
    {
        private CancellationTokenSource cancelTokenSource;

        [TestInitialize]
        public void InitTest()
        {
            cancelTokenSource = new CancellationTokenSource();
        }

        /*
         *   node1 ----> node3
         *     |          /|\
         *     L-> node2 --|
         * 
         */
        [TestMethod]
        public void Simple_shortest_path_with_constant_edge_weight_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node2.ConnectWith(tx, node3, graph);

                tx.Commit();
            }

            using(var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathAlgorithm = new DijkstraShortestPath(tx, graph, node1, cancelTokenSource.Token);
                var shortestPathsData = shortestPathAlgorithm.Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node3).ToList();
                shortestNodePath.Should().ContainInOrder(3, 1);
            }
        }

        /*
               *   node1 ----> node3
               *     |          /|\
               *     L-> node2 --|
               * 
               */
        [TestMethod]
        public void Simple_shortest_path_with_varying_edge_weight_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph,10);
                node2.ConnectWith(tx, node3, graph);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathAlgorithm = new DijkstraShortestPath(tx, graph, node1, cancelTokenSource.Token);
                var shortestPathsData = shortestPathAlgorithm.Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node3).ToList();
                shortestNodePath.Should().ContainInOrder(3, 2, 1);
            }
        }
    }
}
