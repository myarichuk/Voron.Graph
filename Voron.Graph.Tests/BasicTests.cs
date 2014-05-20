using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BasicTests : BaseGraphTest
    {
        public System.Threading.CancellationTokenSource CancelTokenSource;
        
        [TestInitialize]
        public void InitTest()
        {
            CancelTokenSource = new System.Threading.CancellationTokenSource();
        }

        [TestCleanup]
        public void CleanupTest()
        {
            CancelTokenSource.Dispose();
        }

        [TestMethod]
        public void Put_edge_between_nodes_in_the_same_session_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node1, node2, node3;

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

                graph.Commands.CreateEdgeBetween(tx, node3, node1);
                graph.Commands.CreateEdgeBetween(tx, node3, node2);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                graph.Commands.CreateEdgeBetween(tx, node2, node2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var adjacentNodes = graph.Queries.GetAdjacentOf(tx, node3).ToList();
                adjacentNodes.Select(x => x.Key).Should().Contain(new[] { node1.Key, node2.Key });
            }
        }




        [TestMethod]
        public void Put_edge_between_nodes_in_different_session_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
                node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                graph.Commands.CreateEdgeBetween(tx, node2, node3);
                graph.Commands.CreateEdgeBetween(tx, node2, node1);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                graph.Commands.CreateEdgeBetween(tx, node2, node2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var adjacentNodes = graph.Queries.GetAdjacentOf(tx, node2);
                adjacentNodes.Select(x => x.Key).Should().Contain(new []{ node1.Key, node2.Key, node3.Key});
            }
        }
     

        [TestMethod]
        public async Task Can_Avoid_Duplicate_Nodes_InParallel_Adds()
        {
            var graph = new GraphStorage("TestGraph",Env);

            Parallel.For(0, 100, i =>
            {
                using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
                {
                    var newNode = graph.Commands.CreateNode(tx, JsonFromValue("newNode" + i));
                    tx.Commit();
                }

            });
           
            var fetchedKeys = new List<long>();

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var nodesList = await graph.AdminQueries.GetAllNodes(tx, CancelTokenSource.Token);
                nodesList.Select(node => node.Key).Should().OnlyHaveUniqueItems();
            }
        }
       
        [TestMethod]
        public void Can_Iterate_On_Nearest_Nodes()
        {
            var graph = new GraphStorage("TestGraph", Env);
            long centerNodeKey = 0;

            centerNodeKey = Create2DepthHierarchy(graph);

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var centerNode = graph.Queries.LoadNode(tx, centerNodeKey);
                var nodeValues = new Dictionary<string, string>();
                var buffer = new byte[100];
                string curEdgeVal;
                string curNodeVal;

                foreach (var curNode in graph.Queries.GetAdjacentOf(tx, centerNode))
                {
                    var curEdge = graph.Queries.GetEdgesBetween(tx, centerNode, curNode).FirstOrDefault();
                    if (curNode == null)
                    {
                        curEdge = graph.Queries.GetEdgesBetween(tx, curNode, centerNode).FirstOrDefault();
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

        private long Create2DepthHierarchy(GraphStorage graph)
        {
            long centerNodeKey = 0;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var centerNode = graph.Commands.CreateNode(tx, JsonFromValue("centerNode"));
                centerNodeKey = centerNode.Key;


                for (var i = 0; i < 5; i++)
                {
                    var curChild = graph.Commands.CreateNode(tx,JsonFromValue("childNode" + i.ToString()));
                    graph.Commands.CreateEdgeBetween(tx, centerNode, curChild, JsonFromValue(i.ToString()));

                    for (var j = 0; j < 5; j++)
                    {
                        var curGrandChild = graph.Commands.CreateNode(tx, JsonFromValue(string.Concat("childNode", i.ToString(), "child", i.ToString())));
                        graph.Commands.CreateEdgeBetween(tx, curChild, curGrandChild,JsonFromValue((i * 10 + j).ToString()));
                    }
                }
                tx.Commit();
            }
            return centerNodeKey;
        }

        [TestMethod]
        public void Load_nonexisting_node_should_return_null() {
            var graph = new GraphStorage("TestGraph", Env);


            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var illegalNode = graph.Commands.CreateNode(tx,JsonFromValue("onlyNode"));
                illegalNode.Should().NotBeNull();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read)) {
                var noneExistingNode = graph.Queries.LoadNode(tx, 1);
                noneExistingNode.Should().BeNull();
            }

        }

        //TODO : investigate why this test causes debug assertion
        [TestMethod]
        public void Get_edges_between_two_nonexisting_nodes_should_return_empty_collection()
        {
            var graph = new GraphStorage("TestGraph", Env);

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = new Node(-1,JsonFromValue("node1"));
                var node2 = new Node(-2,JsonFromValue("node2"));

                var edgesList = graph.Queries.GetEdgesBetween(tx, node1, node2);
                edgesList.Should().BeEmpty();
            }                        
        }


        //TODO : investigate why this test causes debug assertion
        [TestMethod]
        public void Get_edges_between_existing_and_nonexisting_node_should_return_empty_collection()
        {
            var graph = new GraphStorage("TestGraph", Env);
            long node1Id = 0;

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("node1"));

                Assert.IsNotNull(node1);
                node1Id = node1.Key;
                
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                Node node1 = graph.Queries.LoadNode(tx, node1Id);
                Node node2 = new Node(-2, JsonFromValue("node2"));

                var edgesList = graph.Queries.GetEdgesBetween(tx, node1, node2);
                edgesList.Should().BeEmpty();
            }
        }
      
    }
}
