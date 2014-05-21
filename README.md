### Voron.Graph
A lightweight persisted graph library, based on [Voron](https://github.com/ayende/raven.voron/) - a new transactional key value store developed from scratch by [Hibernating Rhinos](http://hibernatingrhinos.com/).<br/>
The aim of this project to supply rich graph library functionality, with persisted graph data, and without the need to load entire graph into memory.

*Note : this project is work in progress and is far from finished*

#### Show me the code
And how do I use it?<br/>
Usage of the library is simple. The following code creates graph, creates hierarchy of objects
and then queries for adjacent nodes of a certain node. <br/>
All code snippets presented here are taken from unit tests with minor adaptation<br/>

```c#
using (var storage = new StorageEnvironment(StorageEnvironmentOptions.CreateMemoryOnly()))
{
  var graph = new GraphStorage("TestGraph", storage);
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
    graph.Commands.CreateEdgeBetween(tx, node2, node2);
    
    tx.Commit();
  }

  using (var tx = graph.NewTransaction(TransactionFlags.Read))
  {
    var adjacentNodes = graph.Queries.GetAdjacentOf(tx, node3).ToList();
    adjacentNodes.Select(x => x.Key).Should().Contain(new[] { node1.Key, node2.Key });
  }
}
```  
<br/>
#### Algorithms
Using algorithm implementations in Voron.Graph is also simple.<br/>
In this code snippet a graph with hierarchy is created, and then with BFS find all nodes that contain either test2 or test4 in their data
*Assume that Env is StorageEnvironment of the Voron that was initialized earlier.*<br/>
```C#
var graph = new GraphStorage("TestGraph", Env);
using (var tx = graph.NewTransaction(TransactionFlags.ReadWrite))
{
  var node1 = graph.Commands.CreateNode(tx, JsonFromValue("test1"));
  var node2 = graph.Commands.CreateNode(tx, JsonFromValue("test2"));
  var node3 = graph.Commands.CreateNode(tx, JsonFromValue("test3"));
  var node4 = graph.Commands.CreateNode(tx, JsonFromValue("test4"));

  graph.Commands.CreateEdgeBetween(tx, node3, node1);
  graph.Commands.CreateEdgeBetween(tx, node3, node2);
  
  graph.Commands.CreateEdgeBetween(tx, node3, node4);
  graph.Commands.CreateEdgeBetween(tx, node2, node2);
  graph.Commands.CreateEdgeBetween(tx, node2, node4);
  graph.Commands.CreateEdgeBetween(tx, node1, node3);

  tx.Commit();
}

using (var tx = graph.NewTransaction(TransactionFlags.Read))
{
  var bfs = new BreadthFirstSearch(graph, CancelTokenSource.Token);
  var nodes = await bfs.FindMany(tx, data => ValueFromJson<string>(data).Contains("2") ||
                                             ValueFromJson<string>(data).Contains("4"));

  nodes.Select(x => ValueFromJson<string>(x.Data)).Should().Contain("test2", "test4");
}
```
