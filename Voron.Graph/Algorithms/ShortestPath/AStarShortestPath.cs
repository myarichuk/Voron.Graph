using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class AStarShortestPath : BaseAlgorithm, ISingleDestinationShortestPath
    {
        private readonly TraversalAlgorithm _traversal;

        private readonly AStarShortestPathVisitor _shortestPathVisitor;
        private readonly Node _rootNode;
        private Func<Node,Node, int> _heuristic;

        public AStarShortestPath(Transaction tx, 
            GraphStorage graphStorage, 
            Node root, 
            Node targetNode,
            Func<Node,Node, int> heuristic,
            CancellationToken cancelToken)
        {
            _rootNode = root;
            _shortestPathVisitor = new AStarShortestPathVisitor(root, targetNode, heuristic);
            _heuristic = heuristic;
            var traversalStore = new PriorityQueueTraversalStore(new TraversalNodeInfoComparer(root, heuristic));
            _traversal = new TraversalAlgorithm(tx, 
                graphStorage, 
                root,
                traversalStore, 
                cancelToken)
            {
                Visitor = _shortestPathVisitor
            };
        }

        public IEnumerable<long> Execute(Node targetNode)
        {
            _traversal.Traverse();
            if (_shortestPathVisitor.HasDiscoveredDestination == false)
                return null;

            return GetShortestPathToNode(_shortestPathVisitor.PreviousNodeInOptimalPath, targetNode);
        }

        public async Task<IEnumerable<long>> ExecuteAsync(Node targetNode)
        {
            await _traversal.TraverseAsync();
            if (_shortestPathVisitor.HasDiscoveredDestination == false)
                return null;

            return GetShortestPathToNode(_shortestPathVisitor.PreviousNodeInOptimalPath, targetNode);
        }

        private IEnumerable<long> GetShortestPathToNode(Dictionary<long, long> previousNodeInOptimalPath, Node targetNode)
        {
            var results = new Stack<long>();
            if (targetNode == null)
                throw new ArgumentNullException("node");

            if (!previousNodeInOptimalPath.ContainsKey(targetNode.Key))
                return results;

            long currentNodeKey = targetNode.Key;
            while (_rootNode.Key != currentNodeKey)
            {
                results.Push(currentNodeKey);
                currentNodeKey = previousNodeInOptimalPath[currentNodeKey];
                if (currentNodeKey == _rootNode.Key)
                    results.Push(currentNodeKey);
            }

            return results;
        }

        private class TraversalNodeInfoComparer : IComparer<TraversalNodeInfo>
        {
            private Func<Node, Node, int> _heuristic;
            private Node _rootNode;
            public TraversalNodeInfoComparer(Node rootNode,Func<Node, Node, int> heuristic)
            {
                _heuristic = heuristic;
                _rootNode = rootNode;
            }

            public int Compare(TraversalNodeInfo first, TraversalNodeInfo second)
            {
                var firstScore = _heuristic(first.CurrentNode,_rootNode);
                var secondScore = _heuristic(second.CurrentNode, _rootNode);
                return firstScore.CompareTo(secondScore);
            }
        }
    }
}
