using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Voron.Graph;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BasicTests : BaseGraphTest
    {
        
        [TestMethod]
        public void Put_edge_between_nodes_in_the_same_session_should_work()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            Node node1, node2, node3;

            using (var session = graph.OpenSession())
            {
                node1 = session.CreateNode(StreamFrom("test1"));
                node2 = session.CreateNode(StreamFrom("test2"));
                node3 = session.CreateNode(StreamFrom("test3"));

                session.CreateEdgeBetween(node3, node1);
                session.CreateEdgeBetween(node3, node2);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                session.CreateEdgeBetween(node2, node2);

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var adjacentNodes = session.GetAdjacentOf(node3).ToList();
                adjacentNodes.Select(x => x.Key).Should().Contain(new[] { node1.Key, node2.Key });
            }
        }




        [TestMethod]
        public void Put_edge_between_nodes_in_different_session_should_work()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            Node node1, node2, node3;
            using (var session = graph.OpenSession())
            {
                node1 = session.CreateNode(StreamFrom("test1"));
                node2 = session.CreateNode(StreamFrom("test2"));
                node3 = session.CreateNode(StreamFrom("test3"));
                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                session.CreateEdgeBetween(node2, node3);
                session.CreateEdgeBetween(node2, node1);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                session.CreateEdgeBetween(node2, node2);

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var adjacentNodes = session.GetAdjacentOf(node2);
                adjacentNodes.Select(x => x.Key).Should().Contain(new []{ node1.Key, node2.Key, node3.Key});
            }
        }

        [TestMethod]
        public void Can_iterate_on_nodes()
        {
            var graph = new GraphEnvironment("TestGraph", Env);

            var nodeValues = new[] { "test1", "test2", "test3" };

            using (var session = graph.OpenSession())
            {
                foreach (var value in nodeValues)
                    session.CreateNode(StreamFrom(value));

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
               using(var iterator = session.IterateNodes())
               {
                   Assert.IsTrue(iterator.TrySeekToBegin());

                   do
                   {                       
                       nodeValues.Should().Contain(StringFrom(iterator.Current.Data));
                   } while (iterator.MoveNext());
               }
            }
        }

        [TestMethod]
        public void Can_iterate_on_edges()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            Tuple<Node,Node>[] edges;
            using (var session = graph.OpenSession())
            {
                var node1 = session.CreateNode(StreamFrom("test1"));
                var node2 = session.CreateNode(StreamFrom("test2"));
                var node3 = session.CreateNode(StreamFrom("test3"));

                edges = new[]{
                    Tuple.Create(node1,node3),
                    Tuple.Create(node1,node2),
                    Tuple.Create(node3,node2)
                };

                foreach (var nodePair in edges)
                    session.CreateEdgeBetween(nodePair.Item1, nodePair.Item2);

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                using(var edgeIterator = session.IterateEdges())
                {
                    Assert.IsTrue(edgeIterator.TrySeekToBegin());

                    do
                    {
                        var edgeKey = edgeIterator.Current.Key;
                        Assert.IsTrue(edges.Any(x => x.Item1.Key == edgeKey.NodeKeyFrom && x.Item2.Key == edgeKey.NodeKeyTo));
                    } while (edgeIterator.MoveNext());
                }
            }
        }

      
    }
}
