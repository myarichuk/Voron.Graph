using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class DijkstraShortestPath : BaseAlgorithm, ISingleSourceShortestPath
    {
        private readonly TraversalAlgorithm _bfs;
        private readonly DijkstraShortestPathVisitor _shortestPathVisitor;
        private readonly Node _rootNode;

        public DijkstraShortestPath(Transaction tx, GraphStorage graphStorage, Node root, CancellationToken cancelToken)
        {
            _rootNode = root;
            _shortestPathVisitor = new DijkstraShortestPathVisitor();
            _bfs = new TraversalAlgorithm(tx, graphStorage, root, TraversalType.BFS, cancelToken)
            {
                Visitor = _shortestPathVisitor
            };
        }

        public ISingleSourceShortestPathResults Execute()
        {
            _bfs.Traverse();

            return new ShortestPathResults
            {
                RootNode = _rootNode,
                DistancesByNode = _shortestPathVisitor.DistancesByNode,
                PreviousNodeInOptimalPath = _shortestPathVisitor.PreviousNodeInOptimalPath
            };
        }

        public async Task<ISingleSourceShortestPathResults> ExecuteAsync()
        {
            await _bfs.TraverseAsync();

            return new ShortestPathResults
            {
                RootNode = _rootNode,
                DistancesByNode = _shortestPathVisitor.DistancesByNode,
                PreviousNodeInOptimalPath = _shortestPathVisitor.PreviousNodeInOptimalPath
            };
        }

        public class ShortestPathResults : ISingleSourceShortestPathResults
        {
            public Node RootNode { get; internal set; }
            public Dictionary<long, long> DistancesByNode { get; internal set; }
            public Dictionary<long, long> PreviousNodeInOptimalPath { get; internal set; }

            public ShortestPathResults()
            {
                DistancesByNode = new Dictionary<long, long>();
                PreviousNodeInOptimalPath = new Dictionary<long, long>();
            }

            public IEnumerable<long> GetShortestPathToNode(Node node)
            {
                var results = new Stack<long>();
                Debug.Assert(RootNode != null);
                if (node == null)
                    throw new ArgumentNullException("node");

                if (!PreviousNodeInOptimalPath.ContainsKey(node.Key))
                    return results;

                long currentNodeKey = node.Key;
                while (RootNode.Key != currentNodeKey)
                {
                    results.Push(currentNodeKey);
                    currentNodeKey = PreviousNodeInOptimalPath[currentNodeKey];
                    if (currentNodeKey == RootNode.Key)
                        results.Push(currentNodeKey);
                }

                return results;
            }
        }
    }
}
