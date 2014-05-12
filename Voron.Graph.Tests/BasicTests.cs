using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BasicTests : BaseGraphTest
    {
        [TestMethod]
        public void Put_nodes_should_work()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));
                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                StringFrom(session.GetNode("n1")).ShouldBeEquivalentTo("test1");
                StringFrom(session.GetNode("n2")).ShouldBeEquivalentTo("test2");
                StringFrom(session.GetNode("n3")).ShouldBeEquivalentTo("test3");
            }
        }

        [TestMethod]
        public void Put_edge_between_nodes_in_the_same_session_should_work()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));

                session.PutEdge("n2", "n1");
                session.PutEdge("n2", "n3");

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var adjacentNodes = session.GetAdjacent("n2");
                adjacentNodes.Should().Contain("n1", "n3");
            }
        }

        [TestMethod]
        public void Can_iterate_on_nodes()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            var nodeData = new Dictionary<string, string>
            {
                {"n1", "test1"},
                {"n2", "test2"},
                {"n3", "test3"},
                {"n4", "test4"},
            };
            using (var session = graph.OpenSession())
            {
                foreach (var data in nodeData)
                    session.PutNode(data.Key, StreamFrom(data.Value));

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
               using(var iterator = session.IterateNodes())
               {
                   Assert.IsTrue(iterator.TrySeekToBegin());

                   do
                   {
                       nodeData.Should().Contain(new KeyValuePair<string, string>(iterator.Current.Key, StringFrom(iterator.Current.Data)));
                   } while (iterator.MoveNext());
               }
            }
        }

        [TestMethod]
        public void Can_iterate_on_edges()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            List<KeyValuePair<string, string>> edgePairs;
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));

                edgePairs = new List<KeyValuePair<string, string>>
                {
                   new KeyValuePair<string,string>("n2","n1"),
                   new KeyValuePair<string,string>("n2","n3"),
                   new KeyValuePair<string,string>("n1","n3"),
                   new KeyValuePair<string,string>("n3","n3"),
                };

                foreach (var pair in edgePairs)
                    session.PutEdge(pair.Key, pair.Value);

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                using(var edgeIterator = session.IterateEdges())
                {
                    Assert.IsTrue(edgeIterator.TrySeekToBegin());

                    do
                    {
                        edgePairs.Should().Contain(new KeyValuePair<string, string>(edgeIterator.Current.KeyFrom, edgeIterator.Current.KeyTo));
                    } while (edgeIterator.MoveNext());
                }
            }
        }

        [TestMethod]
        public void Can_get_edge_data()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));

                session.PutEdge("n2", "n1", StreamFrom("FooBar1"));
                session.PutEdge("n2", "n3", StreamFrom("FooBar2"));
                session.PutEdge("n1", "n3", StreamFrom("FooBar3"));

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                StringFrom(session.GetEdge("n2", "n3")).Should().BeEquivalentTo("FooBar2");
                StringFrom(session.GetEdge("n2", "n1")).Should().BeEquivalentTo("FooBar1");
                StringFrom(session.GetEdge("n1", "n3")).Should().BeEquivalentTo("FooBar3");
            }
        }


        [TestMethod]
        public void Put_edge_between_nodes_in_different_session_should_work()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));
                
                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                session.PutEdge("n2", "n1");
                session.PutEdge("n2", "n3");

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                session.PutEdge("n2", "n2"); 

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var adjacentNodes = session.GetAdjacent("n2");
                adjacentNodes.Should().Contain("n1", "n3", "n2");
            }
        }
    }
}
