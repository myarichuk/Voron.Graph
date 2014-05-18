using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                node1 = session.CreateNode("test1");
                node2 = session.CreateNode("test2");
                node3 = session.CreateNode("test3");

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
                node1 = session.CreateNode("test1");
                node2 = session.CreateNode("test2");
                node3 = session.CreateNode("test3");
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
                    session.CreateNode(value);

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
               using(var iterator = session.IterateNodes())
               {
                   Assert.IsTrue(iterator.TrySeekToBegin());

                   do
                   {                       
                       nodeValues.Should().Contain(iterator.Current.Data.Value<string>("Value"));
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
                var node1 = session.CreateNode("test1");
                var node2 = session.CreateNode("test2");
                var node3 = session.CreateNode("test3");

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

        [TestMethod]
        public void Can_Avoid_Duplicate_Nodes_InParallel_Adds()
        {
            var graph = new GraphEnvironment("TestGraph",Env);

            Parallel.For(0, 100, i =>
            {
                using (var session = graph.OpenSession())
                {
                    var newNode = session.CreateNode("newNode" + i);
                    session.SaveChanges();
                }

            });
           
            var fetchedKeys = new List<long>();

            using (var session = graph.OpenSession())
            using (var nodeIterator = session.IterateNodes())
            {
                Assert.IsTrue(nodeIterator.TrySeekToBegin());

                do
                {
                    Assert.IsFalse(fetchedKeys.Contains(nodeIterator.Current.Key));
                    fetchedKeys.Add(nodeIterator.Current.Key);
                } while (nodeIterator.MoveNext());
            }
            
        }

        //TODO: investigate why this test throws Voron's debug assertion
        [TestMethod]
        public void Can_Iterate_On_Nearest_Nodes()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            long centerNodeKey = 0;

            centerNodeKey = StoreTestData(graph, centerNodeKey);

            using (var session = graph.OpenSession())
            {
                var centerNode = session.LoadNode(centerNodeKey);
                Dictionary<string, string> nodeValues = new Dictionary<string, string>();
                var buffer = new byte[100];
                string curEdgeVal;
                string curNodeVal;

                foreach (var curNode in session.GetAdjacentOf(centerNode))
                {
                    var curEdge = session.GetEdgesBetween(centerNode, curNode).FirstOrDefault();
                    if (curNode == null)
                    {
                        curEdge = session.GetEdgesBetween(curNode, centerNode).FirstOrDefault();
                    }
                    Assert.IsNotNull(curEdge);

                    Assert.AreNotEqual(0, curEdge.Data.Value<int>("Value"));
                    curEdgeVal = curEdge.Data.Value<string>("Value");

                    Assert.AreNotEqual(0, curNode.Data.Value<int>("Value"));
                    curNodeVal = curNode.Data.Value<string>("Value");

                    nodeValues.Add(curNodeVal, curEdgeVal);

                }

                Assert.AreEqual(nodeValues.Count, 5);
            }
        }

        private long StoreTestData(GraphEnvironment graph, long centerNodeKey)
        {
            using (var session = graph.OpenSession())
            {
                var centerNode = session.CreateNode("centerNode");
                centerNodeKey = centerNode.Key;


                for (var i = 0; i < 5; i++)
                {
                    var curChild = session.CreateNode("childNode" + i.ToString());
                    session.CreateEdgeBetween(centerNode, curChild, i.ToString());

                    for (var j = 0; j < 5; j++)
                    {
                        var curGrandChild = session.CreateNode(string.Concat("childNode", i.ToString(), "child", i.ToString()));
                        session.CreateEdgeBetween(curChild, curGrandChild, (i * 10 + j).ToString());
                    }
                }
                session.SaveChanges();
            }
            return centerNodeKey;
        }

      
    }
}
