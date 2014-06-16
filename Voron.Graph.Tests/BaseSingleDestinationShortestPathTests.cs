using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Voron.Graph.Algorithms.ShortestPath;
using Voron.Graph.Extensions;
using FluentAssertions;
using System.Collections.Generic;

namespace Voron.Graph.Tests
{
    //TODO : add more tests
    [TestClass]
    public abstract class BaseSingleDestinationShortestPathTests : BaseGraphTest
    {
        public struct Point
        {
            public int x;
            public int y;
        }

        protected CancellationTokenSource cancelTokenSource;
        protected Dictionary<long, Point> nodeLocations;

        protected abstract ISingleDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode,Node targetNode);

        [TestInitialize]
        public void InitTest()
        {
            cancelTokenSource = new CancellationTokenSource();
            nodeLocations = new Dictionary<long, Point>();
        }

        /*
               *   node1 (0,0) ----> node3 (3,3)
               *     |                    /|\
               *     L-> node2 (-12,-12) --|
               * 
               */
        [TestMethod]
        public void Simple_shortest_path_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));

                nodeLocations[node1.Key] = new Point
                {
                    x = 0,
                    y = 0
                };

                nodeLocations[node2.Key] = new Point
                {
                    x = -12,
                    y = -12
                };
                
                nodeLocations[node3.Key] = new Point
                {
                    x = 3,
                    y = 3
                };

                node1.ConnectWith(tx, node2, graph, 2);
                node1.ConnectWith(tx, node3, graph, 15);
                node2.ConnectWith(tx, node3, graph, 2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1,node3).Execute();

                shortestPath.Should().ContainInOrder(node1.Key, node2.Key, node3.Key);
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

                nodeLocations[node1.Key] = new Point
                {
                    x = 0,
                    y = 0
                };

                nodeLocations[node2.Key] = new Point
                {
                    x = -12,
                    y = -12
                };

              
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1, node2).Execute();

                shortestPath.Should().BeNull();
            }
        }

        /*       ^ node2 (1,10) -> node3 (2,10)
              *      /                     \
              * node1(0,0) ---------------- > node4 (4,0)
              */
        [TestMethod]
        public void Cheaper_and_shortest_path_should_be_preferred()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));

                node1.ConnectWith(tx, node2, graph, 50);
                node2.ConnectWith(tx, node3, graph, 50);
                node3.ConnectWith(tx, node4, graph, 50);

                node1.ConnectWith(tx, node4, graph, 1);

                tx.Commit();
            }

            nodeLocations[node1.Key] = new Point
            {
                x = 0,
                y = 0
            };

            nodeLocations[node2.Key] = new Point
            {
                x = 1,
                y = 10
            };

            nodeLocations[node3.Key] = new Point
            {
                x = 2,
                y = 10
            };

            nodeLocations[node4.Key] = new Point
            {
                x = 4,
                y = 0
            };

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1, node4).Execute();

                shortestPath.Should().ContainInOrder(node1.Key, node4.Key);
            }
        }

        /*       ^ node2 (1,10) -> node3 (2,10)
              *      /                     \
              * node1(0,0) ---------------- > node4 (4,0)
              */
        [TestMethod]
        public void Cheaper_but_farther_path_should_be_preferred2()
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

            nodeLocations[node1.Key] = new Point
            {
                x = 0,
                y = 0
            };

            nodeLocations[node2.Key] = new Point
            {
                x = 1,
                y = 10
            };
            
            nodeLocations[node3.Key] = new Point
            {
                x = 2,
                y = 10
            };
            
            nodeLocations[node4.Key] = new Point
            {
                x = 4,
                y = 0
            };

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1,node4).Execute();

                shortestPath.Should().ContainInOrder(node1.Key, node2.Key, node3.Key, node4.Key);
            }
        }

        /*
               *   node1 ----> node3
               *     |          /|\
               *     L-> node2 --|
               * 
               */
        //in this test the actual distances are small enough so only the weights actually matter
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

                node1.ConnectWith(tx, node2, graph, 1);
                node1.ConnectWith(tx, node3, graph, 10);
                node2.ConnectWith(tx, node3, graph, 1);

                tx.Commit();
            }

            nodeLocations[node1.Key] = new Point
            {
                x = 0,
                y = 0
            };

            nodeLocations[node2.Key] = new Point
            {
                x = 1,
                y = 1
            };

            nodeLocations[node3.Key] = new Point
            {
                x = 2,
                y = 1
            };

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPaths = GetAlgorithm(tx, graph, node1, node3).Execute();
                shortestPaths.Should().ContainInOrder(node1.Key, node2.Key, node3.Key);
            }
        }

        /*
         *   node1 ----> node3 <----------
         *     |                         |
         *     L-> node2  --> node4 --> node5
         */
        [TestMethod]
        public void Simple_shortest_path_with_equal_weights_and_alternative_path()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4, node5;
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

            nodeLocations[node1.Key] = new Point
            {
                x = 0,
                y = 0
            };

            nodeLocations[node2.Key] = new Point
            {
                x = 1,
                y = 0
            };

            nodeLocations[node3.Key] = new Point
            {
                x = 1,
                y = 0
            };
           
            nodeLocations[node4.Key] = new Point
            {
                x = 1,
                y = 1
            };
            
            nodeLocations[node5.Key] = new Point
            {
                x = 1,
                y = 2
            };

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1, node3).Execute();
                shortestPath.Should().ContainInOrder(node1.Key, node3.Key);
            }
        }
    }
}
