using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.MaximumFlow;
using Voron.Graph.Algorithms.ShortestPath;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph
{
	public partial class GraphStorage
    {
		/// <summary>
		/// Essentially a 'shortcut' class - provides comfortable way to call for different algorithms and perform advanced actions on the graph
		/// </summary>
		public class GraphAdvanced
		{
			private readonly GraphStorage _parent;

			internal GraphAdvanced(GraphStorage parent)
			{
				_parent = parent;
				ShortestPath = new ShortestPathAlgorithms(_parent);
				MaximumFlow = new MaximumFlowAlgorithms(_parent);
			}

			public void Traverse(Transaction tx,Node rootNode, IVisitor visitor,uint? maxDepth = null, Func<ushort,bool> edgeTypePredicate = null, TraversalType traversalType = TraversalType.BFS)
			{
				var algorithm = new TraversalAlgorithm(tx, _parent, rootNode, traversalType, null)
				{
					Visitor = visitor,
					TraverseDepthLimit = maxDepth,
					EdgeTypePredicate = edgeTypePredicate
				};
				algorithm.Traverse();
			}			

			public ShortestPathAlgorithms ShortestPath { get; private set; }

			public class ShortestPathAlgorithms
			{
				private readonly GraphStorage _parent;

				internal ShortestPathAlgorithms(GraphStorage parent) { _parent = parent; }

				public IEnumerable<long> AStar(Transaction tx, Node root, Node targetNode, Func<Node, Node, double> heuristic)
				{
					var algorithm = new AStarShortestPath(tx, _parent, root, targetNode, heuristic, CancellationToken.None);
					return algorithm.Execute();
				}

				public async Task<IEnumerable<long>> AStarAsync(Transaction tx, Node root, Node targetNode, Func<Node, Node, double> heuristic,CancellationToken token)
				{
					var algorithm = new AStarShortestPath(tx, _parent, root, targetNode, heuristic, token);
					var pathInfo = await algorithm.ExecuteAsync();
					return pathInfo;
				}

				public IEnumerable<long> Dijkstra(Transaction tx, Node root, Node targetNode)
                {
					var algorithm = new DijkstraShortestPath(tx, _parent, root, targetNode, CancellationToken.None);
					return algorithm.Execute();
				}

				public async Task<IEnumerable<long>> DijkstraAsync(Transaction tx, Node root, Node targetNode, CancellationToken token)
				{
					var algorithm = new DijkstraShortestPath(tx, _parent, root, targetNode, CancellationToken.None);
					var pathInfo = await algorithm.ExecuteAsync();
					return pathInfo;
				}

				public IMultiDestinationShortestPathResults Dijkstra(Transaction tx, Node root)
				{
					var algorithm = new DijkstraMultiDestinationShortestPath(tx, _parent, root, CancellationToken.None);
					return algorithm.Execute();
				}

				public async Task<IMultiDestinationShortestPathResults> Dijkstra(Transaction tx, Node root, CancellationToken token)
				{
					var algorithm = new DijkstraMultiDestinationShortestPath(tx, _parent, root, token);
					var multiplePathInfo = await algorithm.ExecuteAsync();
					return multiplePathInfo;
				}

				public IMultiDestinationShortestPathResults BellmanFord(Transaction tx, Node root)
                {
					var algorithm = new BellmanFordMultiDestinationShortestPath(tx, _parent, root, CancellationToken.None);
					return algorithm.Execute();
				}

				public async Task<IMultiDestinationShortestPathResults> BellmanFordAsync(Transaction tx, Node root, CancellationToken token)
				{
					var algorithm = new BellmanFordMultiDestinationShortestPath(tx, _parent, root, token);
					var multiplePathInfo = await algorithm.ExecuteAsync();
					return multiplePathInfo;
				}
			}

			public MaximumFlowAlgorithms MaximumFlow { get; private set; }

			public class MaximumFlowAlgorithms
			{
				private readonly GraphStorage _parent;
				internal MaximumFlowAlgorithms(GraphStorage parent)
				{
					_parent = parent;
				}

				public long EdmondsKarp(Transaction tx, Node sourceNode, Node targetNode, Func<Edge, long> capacity)
				{
					var algorithm = new EdmondsKarpMaximumFlow(tx, _parent, sourceNode, targetNode, capacity);
					return algorithm.MaximumFlow();
				}
				
				public async Task<long> EdmondsKarpAsync(Transaction tx, Node sourceNode, Node targetNode, Func<Edge, long> capacity, CancellationToken token)
				{
					var algorithm = new EdmondsKarpMaximumFlow(tx, _parent, sourceNode, targetNode, capacity);
					var flow = await algorithm.MaximumFlowAsync();
					return flow;
				}
			}
		}
    }
}
