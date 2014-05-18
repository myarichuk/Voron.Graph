using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class ChangeTrackingTests : BaseGraphTest
    {
        [TestMethod]
        public void Calling_SaveChanges_should_persist_edge_changes()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            Node node1, node2;

            using (var session = graph.OpenSession())
            {
                node1 = session.CreateNode(StreamFrom("test1"));
                node2 = session.CreateNode(StreamFrom("test2"));

                session.CreateEdgeBetween(node2, node1);

                session.SaveChanges();
            }
            
            using (var session = graph.OpenSession())
            {
                var edge2to1 = session.GetEdgesBetween(node2, node1).FirstOrDefault();
                edge2to1.Should().NotBeNull();

                edge2to1.Key.Type = 123;

                session.SaveChanges();
            }

            using (var session = graph.OpenSession())
            {
                var edge2to1 = session.GetEdgesBetween(node2, node1).FirstOrDefault();

                edge2to1.Should().NotBeNull();
                edge2to1.Key.Type.Should().Be(123);
            }
        }
    }
}
