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
using Voron.Graph.Exceptions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public abstract class BaseMultiDestinationShortestPathTests : BaseGraphTest
    {
        protected CancellationTokenSource cancelTokenSource;

        protected abstract IMultiDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode);

        [TestInitialize]
        public void InitTest()
        {
            cancelTokenSource = new CancellationTokenSource();
        }

        /*
         *   node1 ----> node3
         *     |          ^ 
         *     L-> node2 / 
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

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx,graph,node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node3).ToList();
                shortestNodePath.Should().ContainInOrder(node1.Key, node3.Key);
            }
        }



        /*
         *   node1 ----> node3 <----------
         *     |                         |
         *     L-> node2  --> node4 --> node5
         */
        [TestMethod]
        public void Simple_shortest_path_with_equal_weights_and_alternative_path ()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3,node4,node5;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(1));

                node1.ConnectWith(tx, node2, graph);
                node1.ConnectWith(tx, node3, graph);
                node2.ConnectWith(tx, node4, graph);
                node4.ConnectWith(tx, node5, graph);
                node5.ConnectWith(tx, node3, graph);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node3).ToList();
                shortestNodePath.Should().ContainInOrder(node1.Key, node3.Key);
            }
        }

        [TestMethod]
        public void No_path_between_nodes_should_result_in_null()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));

             
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node2);
                shortestNodePath.Should().BeNull();
            }
        }

        /*
        *   node1 ----> node3 -----> node6 <---
        *     |                               ^
         *    |                               |
        *     L---> node2 -----> node4 --> node5
        */
        [TestMethod]
        public void Simple_shortest_path_with_equal_weights_and_alternative_path2()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5, node6;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node5 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node6 = graph.Commands.CreateNode(tx, JsonFromValue(1));

                node1.ConnectWith(tx, node2, graph,1);
                node1.ConnectWith(tx, node3, graph,15);
                node3.ConnectWith(tx, node6, graph,1);
                node2.ConnectWith(tx, node4, graph,1);
                node4.ConnectWith(tx, node5, graph,1);
                node5.ConnectWith(tx, node6, graph,1);
                
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node6).ToList();
                shortestNodePath.Should().ContainInOrder(node1.Key,node2.Key, node4.Key, node5.Key);
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

                node1.ConnectWith(tx, node2, graph, 2);
                node1.ConnectWith(tx, node3, graph, 10);
                node2.ConnectWith(tx, node3, graph, 2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node3).ToList();
                shortestNodePath.Should().ContainInOrder(node1.Key, node2.Key, node3.Key);
            }
        }


        /*
         *       ^ node2 
         *      /        \
         * node1          > node4
         *      \> node3 /
         * 
         */
        [TestMethod]
        public void Between_two_equivalent_paths_first_created_should_be_chosen()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));

                node1.ConnectWith(tx, node2, graph, 2);
                node2.ConnectWith(tx, node4, graph, 1);

                node1.ConnectWith(tx, node3, graph, 1);
                node3.ConnectWith(tx, node4, graph, 2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node4).ToList();
                shortestNodePath.Should().ContainInOrder(node1.Key, node2.Key, node4.Key);
            }
        }

        /*       ^ node2 -> node3 
         *      /                \
         * node1 ---------------- > node4
         */
        [TestMethod]
        public void Cheap_long_path_over_short_expensive_path_should_be_chosen()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));

                node1.ConnectWith(tx, node2, graph, 1);
                node2.ConnectWith(tx, node3, graph, 1);
                node3.ConnectWith(tx, node4, graph, 1);

                node1.ConnectWith(tx, node4, graph, 10);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathsData = GetAlgorithm(tx, graph, node1).Execute();

                var shortestNodePath = shortestPathsData.GetShortestPathToNode(node4);
                shortestNodePath.Should().ContainInOrder(node1.Key, node2.Key, node3.Key, node4.Key);
            }
        }
    }
}
