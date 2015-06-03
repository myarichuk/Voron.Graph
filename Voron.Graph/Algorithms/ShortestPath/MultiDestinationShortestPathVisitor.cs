using System;
using System.Collections.Generic;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
	public class MultiDestinationShortestPathVisitor : IVisitor
    {
        public Dictionary<long, double> DistancesByNode { get; private set; }
        public Dictionary<long, long> PreviousNodeInOptimalPath { get; private set; }

        private readonly Func<Node, Node, double> _h;
        private readonly Func<TraversalNodeInfo, NodeWithEdge, double> _g;
        private TraversalNodeInfo _currentTraversalNodeInfo;
        private readonly Node _rootNode;

        public MultiDestinationShortestPathVisitor(Node rootNode,
            Func<Node, Node, double> h,
            Func<TraversalNodeInfo, NodeWithEdge, double> g)
        {
            _rootNode = rootNode;
            DistancesByNode = new Dictionary<long, double>();
            PreviousNodeInOptimalPath = new Dictionary<long, long>();
            _h = h;
            _g = g;
        }

        public void DiscoverAdjacent(NodeWithEdge adjacentNodeInfo)
        {
            ValidateAdjacentNodeThrowIfNeeded(adjacentNodeInfo);
            var estimation = _g(_currentTraversalNodeInfo, adjacentNodeInfo) + _h(_rootNode, adjacentNodeInfo.Node);
            var currentNodeKey = adjacentNodeInfo.Node.Key;

            var updateOptimalPath = false;
            if (!DistancesByNode.ContainsKey(adjacentNodeInfo.Node.Key))
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

        public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            _currentTraversalNodeInfo = traversalNodeInfo;
        }

        public bool ShouldStopTraversal
        {
            get
            {
                return false;
            }
        }

        public bool ShouldSkipAdjacentNode(NodeWithEdge adjacentNode)
        {
            return false;
        }

        protected virtual void ValidateAdjacentNodeThrowIfNeeded(NodeWithEdge adjacentNode)
        {
        }
    }
}
