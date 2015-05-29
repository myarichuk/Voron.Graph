using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BasicTests : BaseGraphTest
    {
        public CancellationTokenSource CancelTokenSource;
        
        [TestInitialize]
        public void InitTest()
        {
            CancelTokenSource = new CancellationTokenSource();
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
                node1 = graph.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.CreateNode(tx, JsonFromValue("test2"));
                node3 = graph.CreateNode(tx, JsonFromValue("test3"));

                graph.CreateEdgeBetween(tx, node3, node1);
                graph.CreateEdgeBetween(tx, node3, node2);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                graph.CreateEdgeBetween(tx, node2, node2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var adjacentNodes = graph.GetAdjacentOf(tx, node3).ToList();
                adjacentNodes.Select(x => x.Node.Key).Should().Contain(new[] { node1.Key, node2.Key });
            }
        }




        [TestMethod]
        public void Put_edge_between_nodes_in_different_session_should_work()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node1, node2, node3;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.CreateNode(tx, JsonFromValue("test2"));
                node3 = graph.CreateNode(tx, JsonFromValue("test3"));
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                graph.CreateEdgeBetween(tx, node2, node3);
                graph.CreateEdgeBetween(tx, node2, node1);

                //looping edge also ok!
                //adding multiple loops will overwrite each other
                //TODO: add support for multiple edges that have the same keyFrom and keyTo
                graph.CreateEdgeBetween(tx, node2, node2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var adjacentNodes = graph.GetAdjacentOf(tx, node2);
                adjacentNodes.Select(x => x.Node.Key).Should().Contain(new []{ node1.Key, node2.Key, node3.Key});
            }
        }

        [TestMethod]
        public void Edge_predicate_should_filter_edge_types_correctly()
        {
            var graph = new GraphStorage("TestGraph", Env);
            Node node1, node2, node3, node4, node5;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                node1 = graph.CreateNode(tx, JsonFromValue("test1"));
                node2 = graph.CreateNode(tx, JsonFromValue("test2"));
                node3 = graph.CreateNode(tx, JsonFromValue("test3"));
                node4 = graph.CreateNode(tx, JsonFromValue("test4"));
                node5 = graph.CreateNode(tx, JsonFromValue("test5"));
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                graph.CreateEdgeBetween(tx, node1, node2,type: 1);
                graph.CreateEdgeBetween(tx, node1, node3, type: 2);
                graph.CreateEdgeBetween(tx, node1, node4, type: 3);
                graph.CreateEdgeBetween(tx, node1, node5, type: 2);

                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var adjacentNodes = graph.GetAdjacentOf(tx, node1, type => type == 2);
                adjacentNodes.Select(x => x.Node.Key).Should().Contain(new[] { node3.Key, node5.Key });
            }
        }

        [TestMethod]
        public void Can_Avoid_Duplicate_Nodes_In_Parallel_Adds()
        {
            var graph = new GraphStorage("TestGraph",Env);

            Parallel.For(0, 100, i =>
            {
                using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
                {
	                graph.CreateNode(tx, JsonFromValue("newNode" + i));
	                tx.Commit();
                }
            });
			
            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var nodesList = graph.Admin.GetAllNodes(tx, CancelTokenSource.Token).ToList();
                nodesList.Select(node => node.Key).Should().OnlyHaveUniqueItems();
            }
        }      

        [TestMethod]
        public void Can_Iterate_On_Nearest_Nodes()
        {
            var graph = new GraphStorage("TestGraph", Env);
	        var centerNodeKey = Create2DepthHierarchy(graph);

            using (var tx = graph.NewTransaction(TransactionFlags.Read))
            {
                var centerNode = graph.LoadNode(tx, centerNodeKey);
                var nodeValues = new Dictionary<string, string>();
				var adjacentNodes = graph.GetAdjacentOf(tx, centerNode).Select(x => x.Node).ToList();

				foreach (var curNode in adjacentNodes)
                {
                    var curEdge = graph.GetEdgesBetween(tx, centerNode, curNode).FirstOrDefault();              
                    Assert.IsNotNull(curEdge);

                    var curEdgeVal = curEdge.Data.Value<string>("Value");
                    var curNodeVal = curNode.Data.Value<string>("Value");

                    nodeValues.Add(curNodeVal, curEdgeVal);

                }

                nodeValues.Values.Should().NotContain("grandchild");
                Assert.AreEqual(nodeValues.Count, 5);
            }
        }

        private long Create2DepthHierarchy(GraphStorage graph)
        {
            long centerNodeKey;
            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var centerNode = graph.CreateNode(tx, JsonFromValue("centerNode"));
                centerNodeKey = centerNode.Key;


                for (var i = 1; i < 6; i++)
                {
                    var curChild = graph.CreateNode(tx,JsonFromValue("childNode" + i.ToString()));
                    graph.CreateEdgeBetween(tx, centerNode, curChild, JsonFromValue(i.ToString()));

                    for (var j = 1; j < 6; j++)
                    {
                        var curGrandChild = graph.CreateNode(tx, JsonFromValue(string.Concat("grandchild", i.ToString(), "child", i.ToString())));
                        graph.CreateEdgeBetween(tx, curChild, curGrandChild,JsonFromValue((i * 10 + j).ToString()));
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
                var illegalNode = graph.CreateNode(tx,JsonFromValue("onlyNode"));
                illegalNode.Should().NotBeNull();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.Read)) {
                var noneExistingNode = graph.LoadNode(tx, 1);
                noneExistingNode.Should().BeNull();
            }

        }

        [TestMethod]
        public void Get_edges_between_two_nonexisting_nodes_should_return_empty_collection()
        {
            var graph = new GraphStorage("TestGraph", Env);

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = new Node(-1,JsonFromValue("node1"));
                var node2 = new Node(-2,JsonFromValue("node2"));

                var edgesList = graph.GetEdgesBetween(tx, node1, node2);
                edgesList.Should().BeEmpty();
            }                        
        }


        [TestMethod]
        public void Get_edges_between_existing_and_nonexisting_node_should_return_empty_collection()
        {
            var graph = new GraphStorage("TestGraph", Env);
            long node1Id;

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.CreateNode(tx, JsonFromValue("node1"));

                Assert.IsNotNull(node1);
                node1Id = node1.Key;
                
                tx.Commit();
            }

            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
            {
                var node1 = graph.LoadNode(tx, node1Id);
                var node2 = new Node(-2, JsonFromValue("node2"));

                var edgesList = graph.GetEdgesBetween(tx, node1, node2);
                edgesList.Should().BeEmpty();
            }
        }
      
    }
}
