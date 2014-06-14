using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.ShortestPath;
using Voron.Graph.Extensions;
using FluentAssertions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class AStarShortestPathTests : BaseSingleDestinationShortestPathTests
    {
        protected override ISingleDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode,Node targetNode)
        {
            return new AStarShortestPath(tx, graph, rootNode, targetNode, 
                (nodeFrom, nodeTo) => 
                {
                    //euclidean distance
                    var nodeToLocation = nodeLocations[nodeTo.Key];
                    var nodeFromLocation = nodeLocations[nodeFrom.Key];

                    return Math.Sqrt(Math.Pow(nodeToLocation.y - nodeFromLocation.y, 2) + Math.Pow(nodeToLocation.x - nodeFromLocation.x, 2));
                }, cancelTokenSource.Token);
        }


        /*
         *  node1 (0,0) (5)-> node2 (1,1) (5)-> node3 (2,0)
         *   | (1)                              /|\ (1)
         *   L-----------------node4 (1,-10)---| 
         */
        [TestMethod]
        public void Cheaper_but_farther_path_should_be_preferred()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3, node4;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));
                node4 = graph.Commands.CreateNode(tx, JsonFromValue(4));

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
                    y = 0
                };
                
                nodeLocations[node4.Key] = new Point
                {
                    x = 1,
                    y = -10
                };

                node1.ConnectWith(tx, node2, graph, 10);
                node2.ConnectWith(tx, node3, graph, 10);
                node1.ConnectWith(tx, node4, graph, 1);
                node4.ConnectWith(tx, node3, graph, 1);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPath = GetAlgorithm(tx, graph, node1, node3).Execute();

                shortestPath.Should().ContainInOrder(node1.Key, node4.Key, node3.Key);
            }
        }
    }
}
