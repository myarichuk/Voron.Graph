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
        public void Adding_nodes_should_work()
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
        public void Add_edge_between_nodes_in_the_same_session_should_work()
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
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var nodes = session.Nodes.ToList();

                nodes.Should().NotBeEmpty();
                Assert.IsTrue(nodes.Any(x => x.Key.Equals("n1") && StringFrom(x.Data).Equals("test1")));
                Assert.IsTrue(nodes.Any(x => x.Key.Equals("n2") && StringFrom(x.Data).Equals("test2")));
                Assert.IsTrue(nodes.Any(x => x.Key.Equals("n3") && StringFrom(x.Data).Equals("test3")));

            }
        }

        [TestMethod]
        public void Can_iterate_on_edges()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));

                session.PutEdge("n2", "n1");
                session.PutEdge("n2", "n3");
                session.PutEdge("n1", "n3");

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var edges = session.Edges.ToList();

                edges.Should().NotBeEmpty();
                Assert.IsTrue(edges.Any(x => x.KeyFrom.Equals("n2") && x.KeyTo.Equals("n3")));
                Assert.IsTrue(edges.Any(x => x.KeyFrom.Equals("n1") && x.KeyTo.Equals("n3")));
                Assert.IsTrue(edges.Any(x => x.KeyFrom.Equals("n2") && x.KeyTo.Equals("n1")));

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
        public void Add_edge_between_nodes_in_different_session_should_work()
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
