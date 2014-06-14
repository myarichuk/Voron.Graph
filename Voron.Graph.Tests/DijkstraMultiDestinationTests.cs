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
    //TODO: add more tests
    [TestClass]
    public class DijkstraMultiDestinationTests : BaseMultiDestinationShortestPathTests
    {

        [TestMethod]
        public void Simple_shortest_path_with_negative_edge_weight_should_throw()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue(1));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue(2));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue(3));

                node1.ConnectWith(tx, node2, graph, -1);
                node1.ConnectWith(tx, node3, graph, -1);
                node2.ConnectWith(tx, node3, graph, -1);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var shortestPathAlgorithm = new DijkstraMultiDestinationShortestPath(tx, graph, node1, cancelTokenSource.Token);
                var shortestPathsData = shortestPathAlgorithm.Invoking(x => x.Execute())
                                                             .ShouldThrow<AlgorithmConstraintException>();

            }
        }

        protected override IMultiDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode)
        {
            return new DijkstraMultiDestinationShortestPath(tx, graph, rootNode, cancelTokenSource.Token);
        }
    }
}
