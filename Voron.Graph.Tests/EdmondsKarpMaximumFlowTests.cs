using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.MaximumFlow;
using Voron.Graph.Extensions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class EdmondsKarpMaximumFlowTests : BaseGraphTest
    {
        /*
         *     > u >
         *   /      \
         *  s        >t
         *   \      /
         *     > v /
         */
        [TestMethod]
        public void SimpleNetworkFlow1()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node s, u, v, t;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                s = graph.Commands.CreateNode(tx, JsonFromValue("s"));
                u = graph.Commands.CreateNode(tx, JsonFromValue("u"));
                v = graph.Commands.CreateNode(tx, JsonFromValue("v"));
                t = graph.Commands.CreateNode(tx, JsonFromValue("t"));

                s.ConnectWith(tx, u, graph, 20);
                u.ConnectWith(tx, t, graph, 10);
                s.ConnectWith(tx, v, graph, 10);
                v.ConnectWith(tx, t, graph, 20);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpAlgorithm(tx, graph, s, t, e => e.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(20, maximumFlow);
            }
        }
 
        /*
         *     > u >
         *   /   |  \
         *  s    |   >t
         *   \  \|/ /
         *     > v /
         */
        [TestMethod]
        public void SimpleNetworkFlow2()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node s, u, v, t;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                s = graph.Commands.CreateNode(tx, JsonFromValue("s"));
                u = graph.Commands.CreateNode(tx, JsonFromValue("u"));
                v = graph.Commands.CreateNode(tx, JsonFromValue("v"));
                t = graph.Commands.CreateNode(tx, JsonFromValue("t"));

                s.ConnectWith(tx, u, graph, 20);
                u.ConnectWith(tx, t, graph, 10);
                s.ConnectWith(tx, v, graph, 10);
                v.ConnectWith(tx, t, graph, 20);
                u.ConnectWith(tx, v, graph, 30);

                tx.Commit();
            }
            
            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpAlgorithm(tx, graph, s, t, e => e.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(30, maximumFlow);
            }
        }
    }
}
