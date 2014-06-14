using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class AStarShortestPathVisitor : SingleDestinationShortestPathVisitor
    {
        private readonly HashSet<long> _openSet;
        private readonly HashSet<long> _closedSet;
        public AStarShortestPathVisitor(Node rootNode,
            Node targetNode,
            Func<Node, Node, double> h,
            Func<TraversalNodeInfo, NodeWithEdge, double> g)
            :base(rootNode,targetNode,h,g)
        {
            _openSet = new HashSet<long>();
            _closedSet = new HashSet<long>();
            _openSet.Add(rootNode.Key);
        }

        public override void DiscoverAdjacent(NodeWithEdge neighboorNode)
        {
            _openSet.Add(neighboorNode.Node.Key);
            base.DiscoverAdjacent(neighboorNode);
        }

        public override void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            _openSet.Remove(traversalNodeInfo.CurrentNode.Key);
            _closedSet.Add(traversalNodeInfo.CurrentNode.Key);
            base.ExamineTraversalInfo(traversalNodeInfo);
        }

        public override bool ShouldSkipAdjacentNode(NodeWithEdge adjacentNode)
        {
            return _closedSet.Contains(adjacentNode.Node.Key);
        }

        public override bool ShouldStopTraversal
        {
            get
            {
                return base.ShouldStopTraversal && _openSet.Count == 0;
            }
        }
    }
}
