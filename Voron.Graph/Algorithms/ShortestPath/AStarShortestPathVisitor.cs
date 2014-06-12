
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class AStarShortestPathVisitor : IVisitor
    {
        private bool _hasDiscoveredDestination;

        public Dictionary<long, long> DistancesByNode;
        public Dictionary<long, long> PreviousNodeInOptimalPath;

        private readonly Func<Node, Node, int> _heuristic;
        private TraversalNodeInfo _currentTraversalNodeInfo;
        private readonly Node _rootNode;
        private readonly Node _targetNode;

        public AStarShortestPathVisitor(Node rootNode, Node targetNode, Func<Node, Node, int> heuristic)
        {
            _hasDiscoveredDestination = false;
            _rootNode = rootNode;
            _targetNode = targetNode;
            DistancesByNode = new Dictionary<long, long>();
            PreviousNodeInOptimalPath = new Dictionary<long, long>();
            _heuristic = heuristic;
        }

        public void DiscoverAdjacent(Primitives.NodeWithEdge neighboorNode)
        {
            var alt = _currentTraversalNodeInfo.TotalEdgeWeightUpToNow + neighboorNode.EdgeTo.Weight + _heuristic(_rootNode,neighboorNode.Node);
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

            if (updateOptimalPath)
                PreviousNodeInOptimalPath[currentNodeKey] = _currentTraversalNodeInfo.CurrentNode.Key;
        }

        public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            _currentTraversalNodeInfo = traversalNodeInfo;
            if (_currentTraversalNodeInfo.CurrentNode.Key == _targetNode.Key)
                _hasDiscoveredDestination = true;
        }

        public bool ShouldStopTraversal
        {
            get 
            {
                return _hasDiscoveredDestination;
            }
        }

        public bool HasDiscoveredDestination
        {
            get
            {
                return _hasDiscoveredDestination;
            }
        }

        public bool ShouldSkipAdjacentNode(Primitives.NodeWithEdge adjacentNode)
        {
            return false;
        }
    }
}
