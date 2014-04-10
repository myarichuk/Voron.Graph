using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BFSTests : BaseGraphTest
    {

        [TestMethod]
        public void TestFindInSparselyConnectedGraph()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            using (var session = graph.OpenSession())
            {
                session.PutNode("n1", StreamFrom("test1"));
                session.PutNode("n2", StreamFrom("test2"));
                session.PutNode("n3", StreamFrom("test3"));
                session.PutNode("n4", StreamFrom("test4"));
                session.PutNode("n5", StreamFrom("test5"));

                session.PutEdge("n1", "n2");
                session.PutEdge("n2", "n3");
                session.PutEdge("n2", "n4");

                session.SaveChanges();
            }

            //search among the connected nodes
            var searchResult = graph.FindOne((k,v) => k.Equals("n4"));
            Assert.IsNotNull(searchResult);
            Assert.AreEqual("test4", StringFrom(searchResult));

            //search on disconnected node
            searchResult = graph.FindOne((k,v) => k.Equals("n5"));
            Assert.IsNotNull(searchResult);
            Assert.AreEqual("test5", StringFrom(searchResult));
        }
    }
}
