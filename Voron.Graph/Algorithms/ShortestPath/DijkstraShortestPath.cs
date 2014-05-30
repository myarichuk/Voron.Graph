using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class DijkstraShortestPath : BaseAlgorithm
    {
        private readonly TraversalAlgorithm _bfs;
        private readonly Dictionary<long, long> _distancesByNodeKey;
        private readonly Dictionary<long, long> _previousOptimalNodeKey;
        private readonly ShortestPathVisitor _shortestPathVisitor;
        private readonly Node _rootNode;

        public DijkstraShortestPath(Transaction tx, GraphStorage graphStorage, Node root, CancellationToken cancelToken)
        {
            _rootNode = root;
            _shortestPathVisitor = new ShortestPathVisitor();
            _bfs = new TraversalAlgorithm(tx, graphStorage, root, TraversalType.BFS, cancelToken)
            {
                Visitor = _shortestPathVisitor
            };
            _distancesByNodeKey = new Dictionary<long, long>();
            _previousOptimalNodeKey = new Dictionary<long, long>();

            _distancesByNodeKey[root.Key] = 0;           
        }

        public Results Execute()
        {
            _bfs.Traverse();

            return new Results
            {
                RootNode = _rootNode,
                DistancesByNode = _shortestPathVisitor.DistancesByNode,
                PreviousNodeInOptimalPath = _shortestPathVisitor.PreviousNodeInOptimalPath
            };
        }

        public async Task<Results> ExecuteAsync()
        {
            await _bfs.TraverseAsync();

            return new Results
            {
                RootNode = _rootNode,
                DistancesByNode = _shortestPathVisitor.DistancesByNode,
                PreviousNodeInOptimalPath = _shortestPathVisitor.PreviousNodeInOptimalPath
            };
        }

        public class Results
        {
            public Node RootNode { get; internal set; }
            public Dictionary<long, long> DistancesByNode { get; internal set; }
            public Dictionary<long, long> PreviousNodeInOptimalPath { get; internal set; }

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

        private class ShortestPathVisitor : IVisitor
        {
            public Dictionary<long, long> DistancesByNode;
            public Dictionary<long, long> PreviousNodeInOptimalPath;

            private TraversalNodeInfo currentTraversalNodeInfo;

            public ShortestPathVisitor()
            {
                DistancesByNode = new Dictionary<long, long>();
                PreviousNodeInOptimalPath = new Dictionary<long, long>();
            }

            public void DiscoverAdjacent(NodeWithEdge neighboorNode)
            {
                //ignore loops
                if (neighboorNode.EdgeTo.Key.NodeKeyFrom == neighboorNode.EdgeTo.Key.NodeKeyTo)
                    return;

                var alt = currentTraversalNodeInfo.TotalEdgeWeightUpToNow + neighboorNode.EdgeTo.Weight;
                var currentNodeKey = neighboorNode.Node.Key;

                bool updateOptimalPath = false;
                if (!DistancesByNode.ContainsKey(neighboorNode.Node.Key))
                {
                    DistancesByNode.Add(currentNodeKey, alt);
                    updateOptimalPath = true;
                }
                else if (DistancesByNode[currentNodeKey] > alt)
                {
                    DistancesByNode[currentNodeKey] = alt;
                    updateOptimalPath = true;
                }

                if(updateOptimalPath)
                    PreviousNodeInOptimalPath[currentNodeKey] = currentTraversalNodeInfo.CurrentNode.Key;
            }

            public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
            {
                currentTraversalNodeInfo = traversalNodeInfo;
            }


            public bool ShouldStopTraversal
            {
                get { return false; }
            }
        }
    }
}
