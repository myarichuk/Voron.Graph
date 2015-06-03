using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                s = graph.CreateNode(tx, JsonFromValue("s"));
                u = graph.CreateNode(tx, JsonFromValue("u"));
                v = graph.CreateNode(tx, JsonFromValue("v"));
                t = graph.CreateNode(tx, JsonFromValue("t"));

                s.ConnectWith(tx, u, graph, 20);
                u.ConnectWith(tx, t, graph, 10);
                s.ConnectWith(tx, v, graph, 10);
                v.ConnectWith(tx, t, graph, 20);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpMaximumFlow(tx, graph, s, t, e => e.Weight);
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
			SimpleNetworkFlowParametric(20, 30, 30);
        }

		/*
         *     > u >
         *   /   |  \
         *  s    |   >t
         *   \  \|/ /
         *     > v /
         */
        [TestMethod]
        public void SimpleNetworkFlow3()
        {
			SimpleNetworkFlowParametric(10, 60, 20);
        }

		private void SimpleNetworkFlowParametric(short weightOfVtoTEdge, short weightOfUtoVEdge, int expected)
		{
			var graph = new GraphStorage("TestGraph", Env);
			Node s, u, v, t;
			using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
			{
				s = graph.CreateNode(tx, JsonFromValue("s"));
				u = graph.CreateNode(tx, JsonFromValue("u"));
				v = graph.CreateNode(tx, JsonFromValue("v"));
				t = graph.CreateNode(tx, JsonFromValue("t"));
				s.ConnectWith(tx, u, graph, 20);
				u.ConnectWith(tx, t, graph, 10);
				s.ConnectWith(tx, v, graph, 10);
				v.ConnectWith(tx, t, graph, weightOfVtoTEdge);
				u.ConnectWith(tx, v, graph, weightOfUtoVEdge);
				tx.Commit();
			}
			using (var tx = graph.NewTransaction(TransactionFlags.Read))
			{
				var algorithm = new EdmondsKarpMaximumFlow(tx, graph, s, t, e => e.Weight);
				var maximumFlow = algorithm.MaximumFlow();
				Assert.AreEqual(expected, maximumFlow);
			}
		}

        [TestMethod]
        public void SimpleNetworkFlow4()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node a, b;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                a = graph.CreateNode(tx, JsonFromValue("a"));
                b = graph.CreateNode(tx, JsonFromValue("b"));

                a.ConnectWith(tx, b, graph, 25);
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpMaximumFlow(tx, graph, a, b, edge => edge.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(25, maximumFlow);
            }
        }

        [TestMethod]
        public void In_disconnected_network_should_return_zero_max_flow()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node a, b;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                a = graph.CreateNode(tx, JsonFromValue("a"));
                b = graph.CreateNode(tx, JsonFromValue("b"));
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpMaximumFlow(tx, graph, a, b, edge => edge.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(0, maximumFlow);
            }
        }

        [TestMethod]
        public void In_network_with_zero_flow_should_return_zero_max_flow()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node a, b;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                a = graph.CreateNode(tx, JsonFromValue("a"));
                b = graph.CreateNode(tx, JsonFromValue("b"));

                a.ConnectWith(tx, b, graph, 0);
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpMaximumFlow(tx, graph, a, b, edge => edge.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(0, maximumFlow);
            }
        }

        [TestMethod]
        public void ComplexNetworkFlow1()
        {
            var graph = new GraphStorage("TestGraph", Env);

            Node a,b,c,d,e,f,g;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                a = graph.CreateNode(tx, JsonFromValue("a"));
                b = graph.CreateNode(tx, JsonFromValue("b"));
                c = graph.CreateNode(tx, JsonFromValue("c"));
                d = graph.CreateNode(tx, JsonFromValue("d"));
                e = graph.CreateNode(tx, JsonFromValue("e"));
                f = graph.CreateNode(tx, JsonFromValue("f"));
                g = graph.CreateNode(tx, JsonFromValue("g"));

                a.ConnectWith(tx, b, graph, 3);
                a.ConnectWith(tx, d, graph, 3);
                b.ConnectWith(tx, c, graph, 4);
                c.ConnectWith(tx, a, graph, 3);
                c.ConnectWith(tx, d, graph, 1);
                c.ConnectWith(tx, e, graph, 2);
                e.ConnectWith(tx, b, graph, 1);
                d.ConnectWith(tx, e, graph, 2);
                d.ConnectWith(tx, f, graph, 6);
                f.ConnectWith(tx, g, graph, 9);
                e.ConnectWith(tx, g, graph, 1);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var algorithm = new EdmondsKarpMaximumFlow(tx, graph, a, g, edge => edge.Weight);
                var maximumFlow = algorithm.MaximumFlow();
                Assert.AreEqual(5, maximumFlow);
            }
        }
    }
}
