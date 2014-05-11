using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
                StringFrom(session.Get("n1")).ShouldBeEquivalentTo("test1");
                StringFrom(session.Get("n2")).ShouldBeEquivalentTo("test2");
                StringFrom(session.Get("n3")).ShouldBeEquivalentTo("test3");
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

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var adjacentNodes = session.GetAdjacent("n2");
                adjacentNodes.Should().Contain("n1", "n3");
            }
        }
    }
}
