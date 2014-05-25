using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using System.Threading;
using Voron.Graph.Extensions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class EtagTests : BaseGraphTest
    {
        [TestMethod]
        public void Concurrent_etag_creations_should_result_in_unique_etags()
        {
            var etags = new ConcurrentBag<Etag>();

            Parallel.For(0, 1000, i =>
            {
                for (int j = 0; j < 1000; j++)
                    etags.Add(Etag.Generate());
            });

            etags.Should().OnlyHaveUniqueItems("Because concurrently created Etags must be unique");            
        }

        [TestMethod]
        public void Older_etag_should_be_smaller_in_comarison_to_recently_created1()
        {
            var oldEtag = Etag.Generate();
            Thread.Sleep(20); //DateTime.UtcNow has 15ms resolution
            var recentEtag1 = Etag.Generate();
            Thread.Sleep(20); //DateTime.UtcNow has 15ms resolution
            var recentEtag2 = Etag.Generate();

            Assert.IsTrue(recentEtag1 > oldEtag);
            Assert.IsTrue(recentEtag2 > recentEtag1);
            Assert.IsTrue(recentEtag1 != oldEtag);
        }

        [TestMethod]
        public void Older_etag_should_be_smaller_in_comarison_to_recently_created2()
        {
            var oldEtag = Etag.Generate();
            var recentEtag1 = Etag.Generate();
            var recentEtag2 = Etag.Generate();

            Assert.IsTrue(recentEtag1 > oldEtag);
            Assert.IsTrue(recentEtag2 > recentEtag1);
            Assert.IsTrue(recentEtag1 != oldEtag);
        }

        [TestMethod]
        public void Created_nodes_should_have_incremental_etags()
        {
            var graph = new GraphStorage("TestGraph", Env); 
            var nodes = new List<Node>();

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                for (int i = 0; i < 100; i++)
                    nodes.Add(graph.Commands.CreateNode(tx, JsonFromValue("test1")));
                tx.Commit();
            }

            nodes.Select(x => x.Etag).Should().OnlyHaveUniqueItems()
                                              .And
                                              .BeInAscendingOrder();
        }

        [TestMethod]
        public void Created_nodes_in_parallel_should_have_incremental_etags()
        {
            var graph = new GraphStorage("TestGraph", Env);
            var nodes = new ConcurrentBag<Node>();

            Parallel.For(0, 100, i =>
            {
                using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
                {
                    for (int j = 0; j < 10; j++)
                        nodes.Add(graph.Commands.CreateNode(tx, JsonFromValue("test1")));
                    tx.Commit();
                }
            });
            nodes.OrderBy(x => x.Etag).Select(x => x.Etag).Should()
                                                          .OnlyHaveUniqueItems()
                                                          .And
                                                          .BeInAscendingOrder();
        }

        [TestMethod]
        public void Created_edges_should_have_incremental_etags()
        {
            var graph = new GraphStorage("TestGraph", Env);
            var edges = new List<Edge>();

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));

                for (int i = 0; i < 100; i++)
                    edges.Add(node1.ConnectWith(tx, node2, graph));
                tx.Commit();
            }

            edges.Select(x => x.Etag).Should().OnlyHaveUniqueItems()
                                              .And
                                              .BeInAscendingOrder();
        }


        [TestMethod]
        public void Node_etags_after_update_should_be_incremental()
        {
            Node node;
            var graph = new GraphStorage("TestGraph", Env);
            var etags = new List<Etag>();

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                tx.Commit();
            }

            for(int i = 0; i < 100; i++)
            {
                using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
                {
                    node.Data["value"] = "updated " + i;
                    Assert.IsTrue(graph.Commands.TryUpdate(tx, node));
                    etags.Add(node.Etag);
                    tx.Commit();
                }
            }

            etags.Should().OnlyHaveUniqueItems()
                          .And
                          .BeInAscendingOrder();
        }
    }
}
