using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class SingleDestinationShortestPathVisitor : IVisitor
    {
        private bool _hasDiscoveredDestination;

        public Dictionary<long, double> DistancesByNode { get; private set; }
        public Dictionary<long, long> PreviousNodeInOptimalPath { get; private set; }

        private readonly Func<Node, Node, double> _h;
        private readonly Func<TraversalNodeInfo, NodeWithEdge, double> _g;
        private TraversalNodeInfo _currentTraversalNodeInfo;
        private readonly Node _rootNode;
        private readonly Node _targetNode;

        public Func<bool> IsProcessingQueueEmpty { get; set; }

        public SingleDestinationShortestPathVisitor(Node rootNode,
            Node targetNode, 
            Func<Node, Node, double> h,
            Func<TraversalNodeInfo, NodeWithEdge, double> g)
        {
            _hasDiscoveredDestination = false;
            _rootNode = rootNode;
            _targetNode = targetNode;
            DistancesByNode = new Dictionary<long, double>();
            PreviousNodeInOptimalPath = new Dictionary<long, long>();
            _h = h;
            _g = g;
        }

        public virtual void DiscoverAdjacent(Primitives.NodeWithEdge neighboorNode)
        {
            var estimation = _g(_currentTraversalNodeInfo,neighboorNode) + _h(_rootNode,neighboorNode.Node);
            var currentNodeKey = neighboorNode.Node.Key;

            bool updateOptimalPath = false;
            if (!DistancesByNode.ContainsKey(neighboorNode.Node.Key))
            {
                DistancesByNode.Add(currentNodeKey, estimation);
                updateOptimalPath = true;
            }
            else if (DistancesByNode[currentNodeKey] > estimation)
            {
                DistancesByNode[currentNodeKey] = estimation;
                updateOptimalPath = true;
            }

            if (updateOptimalPath)
                PreviousNodeInOptimalPath[currentNodeKey] = _currentTraversalNodeInfo.CurrentNode.Key;
        }

        public virtual void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            _currentTraversalNodeInfo = traversalNodeInfo;
            if (_targetNode != null && _currentTraversalNodeInfo.CurrentNode.Key == _targetNode.Key)
                _hasDiscoveredDestination = true;
        }

        public virtual bool ShouldStopTraversal
        {
            get 
            {
                return (IsProcessingQueueEmpty == null) ? _hasDiscoveredDestination :
                                    _hasDiscoveredDestination && IsProcessingQueueEmpty();
            }
        }

        public bool HasDiscoveredDestination
        {
            get
            {
                return _hasDiscoveredDestination;
            }
        }

        public virtual bool ShouldSkipAdjacentNode(Primitives.NodeWithEdge adjacentNode)
        {
            return false;
        }
    }
}
