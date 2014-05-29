//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Voron.Graph.Algorithms.Search;
//using FluentAssertions;
//using System.Threading;
//using Voron.Graph.Extensions;

//namespace Voron.Graph.Tests
//{
//    [TestClass]
//    public class SearchTests : BaseGraphTest
//    {
//        public CancellationTokenSource CancelTokenSource;

//        [TestInitialize]
//        public void InitTest()
//        {
//            CancelTokenSource = new CancellationTokenSource();
//        }

//        [TestMethod]
//        public async Task BFS_Traverse_with_depth_limit_should_work()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            Node node1;
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
//                var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

//                graph.Commands.CreateEdgeBetween(tx, node1, node2);
//                graph.Commands.CreateEdgeBetween(tx, node2, node3);
//                graph.Commands.CreateEdgeBetween(tx, node3, node4);

//                tx.Commit();
//            }
//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var results = await searchAlgorithm.FindMany(tx, data => true,node1,searchDepthLimit:2);

//                results.Select(x => x.Key).Should().HaveCount(2)
//                                          .And
//                                          .OnlyContain(x => x == 1 || x == 2);
//            }
//        }

//        [TestMethod]
//        public async Task DFS_Traverse_with_depth_limit_should_work()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            Node node1;
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
//                var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

//                graph.Commands.CreateEdgeBetween(tx, node1, node2);
//                graph.Commands.CreateEdgeBetween(tx, node2, node3);
//                graph.Commands.CreateEdgeBetween(tx, node3, node4);

//                tx.Commit();
//            }
//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var results = await searchAlgorithm.FindMany(tx, data => true, node1, searchDepthLimit: 2);

//                results.Select(x => x.Key).Should().HaveCount(2)
//                                          .And
//                                          .OnlyContain(x => x == 1 || x == 2);
//            }
//        }

//        [TestMethod]
//        public async Task BFS_FindOne_with_connected_root_should_return_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            Node node2;
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                graph.Commands.CreateEdgeBetween(tx, node2, node2);
//                graph.Commands.CreateEdgeBetween(tx, node1, node3);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                node.Should().NotBeNull();
//                node.Key.Should().Be(node2.Key);
//            }
//        }

//        [TestMethod]
//        public async Task DFS_FindOne_with_connected_root_should_return_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            Node node2;
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                graph.Commands.CreateEdgeBetween(tx, node2, node2);
//                graph.Commands.CreateEdgeBetween(tx, node1, node3);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                node.Should().NotBeNull();
//                node.Key.Should().Be(node2.Key);
//            }
//        }

//        [TestMethod]
//        public async Task BFS_FindOne_with_only_root_node_in_graph_returns_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test1"));

//                node.Should().NotBeNull();
//            }
//        }

//        [TestMethod]
//        public void BFS_FindOne_with_request_cancellation_will_return_canceled_search_task()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));

//                graph.Commands.CreateEdgeBetween(tx, node1, node2);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                searchAlgorithm.NodeVisited += node => CancelTokenSource.Cancel();
//                var searchTask = searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                searchTask.ContinueWith(task =>
//                {
//                    task.IsFaulted.Should().BeFalse();
//                    task.IsCanceled.Should().BeTrue();
//                });
//            }
//        }

//        [TestMethod]
//        public void DFS_FindOne_with_request_cancellation_will_return_canceled_search_task()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));

//                graph.Commands.CreateEdgeBetween(tx, node1, node2);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                searchAlgorithm.NodeVisited += node => CancelTokenSource.Cancel();
//                var searchTask = searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                searchTask.ContinueWith(task =>
//                {
//                    task.IsFaulted.Should().BeFalse();
//                    task.IsCanceled.Should().BeTrue();
//                });
//            }
//        }

//        [TestMethod]
//        public async Task DFS_FindOne_with_only_root_node_in_graph_returns_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test1"));

//                node.Should().NotBeNull();
//            }
//        }

//        [TestMethod]
//        public async Task BFS_FindOne_with_disconnected_root_should_return_null()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                //note: root node is selected by using a first node that was added
//                //since Voron.Graph is a directed graph - no node leads from the root node means nothing
//                //can be found

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                node.Should().BeNull();
//            }
//        }

//        [TestMethod]
//        public async Task DFS_FindOne_with_disconnected_root_should_return_null()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                //note: root node is selected by using a first node that was added
//                //since Voron.Graph is a directed graph - no node leads from the root node means nothing
//                //can be found

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var node = await searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test2"));

//                node.Should().BeNull();
//            }
//        }

//        [TestMethod]
//        public async Task BFS_FindMany_with_connected_root_returns_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
//                var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                graph.Commands.CreateEdgeBetween(tx, node3, node4);
//                graph.Commands.CreateEdgeBetween(tx, node2, node2);
//                graph.Commands.CreateEdgeBetween(tx, node2, node4);
//                graph.Commands.CreateEdgeBetween(tx, node1, node3);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var nodes = await searchAlgorithm.FindMany(tx, data => ValueFromJson<string>(data).Contains("2") ||
//                                                           ValueFromJson<string>(data).Contains("4"));

//                nodes.Select(x => ValueFromJson<string>(x.Data)).Should().Contain("test2", "test4");
//            }
//        }

//        [TestMethod]
//        public async Task DFS_FindMany_with_connected_root_returns_correct_results()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
//                var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
//                var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

//                graph.Commands.CreateEdgeBetween(tx, node3, node1);
//                graph.Commands.CreateEdgeBetween(tx, node3, node2);

//                graph.Commands.CreateEdgeBetween(tx, node3, node4);
//                graph.Commands.CreateEdgeBetween(tx, node2, node2);
//                graph.Commands.CreateEdgeBetween(tx, node2, node4);
//                graph.Commands.CreateEdgeBetween(tx, node1, node3);

//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var nodes = await searchAlgorithm.FindMany(tx, data => ValueFromJson<string>(data).Contains("2") ||
//                                                           ValueFromJson<string>(data).Contains("4"));

//                nodes.Select(x => ValueFromJson<string>(x.Data)).Should().Contain("test2", "test4");
//            }
//        }

//        [TestMethod]
//        public void BFS_FindOne_should_throw_exception_if_algorithm_already_runs()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                graph.Commands.CreateNode(tx, JsonFromValue("test1"));                
//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new BreadthFirstSearch(graph, CancelTokenSource.Token);
//                var searchStallEvent = new ManualResetEventSlim();
//                searchAlgorithm.Traverse(tx, 
//                data =>
//                {
//                    searchStallEvent.Wait();
//                    return true;
//                }, 
//                () =>
//                {
//                    searchStallEvent.Wait();
//                    return true;
//                });

//                var findTask = searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test1"));

//                findTask.ContinueWith(task =>
//                    {
//                        task.IsFaulted.Should().BeTrue();
//                        if (task.Exception != null)
//                            task.Exception.InnerExceptions.First().Should().BeOfType<InvalidOperationException>();

//                        searchStallEvent.Set();
//                    });
//            }
//        }

//        [TestMethod]
//        public void DFS_FindOne_should_throw_exception_if_algorithm_already_runs()
//        {
//            var graph = new GraphStorage("TestGraph", Env);
//            using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
//            {
//                graph.Commands.CreateNode(tx, JsonFromValue("test1"));
//                tx.Commit();
//            }

//            using (var tx = graph.NewTransaction(TransactionFlags.Read))
//            {
//                var searchAlgorithm = new DepthFirstSearch(graph, CancelTokenSource.Token);
//                var searchStallEvent = new ManualResetEventSlim();
//                searchAlgorithm.Traverse(tx,
//                data =>
//                {
//                    searchStallEvent.Wait();
//                    return true;
//                },
//                () =>
//                {
//                    searchStallEvent.Wait();
//                    return true;
//                });

//                var findTask = searchAlgorithm.FindOne(tx, data => ValueFromJson<string>(data).Equals("test1"));

//                findTask.ContinueWith(task =>
//                {
//                    task.IsFaulted.Should().BeTrue();
//                    if (task.Exception != null)
//                        task.Exception.InnerExceptions.First().Should().BeOfType<InvalidOperationException>();

//                    searchStallEvent.Set();
//                });
//            }
//        }
//    }
//}
