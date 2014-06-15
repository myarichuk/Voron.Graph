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
        public AStarShortestPathVisitor(Node rootNode,
            Node targetNode,
            Func<Node, Node, double> h,
            Func<TraversalNodeInfo, NodeWithEdge, double> g)
            :base(rootNode,targetNode,h,g)
        {
            _openSet = new HashSet<long>();
            _openSet.Add(rootNode.Key);
        }

        public override void DiscoverAdjacent(NodeWithEdge neighboorNode)
        {
            base.DiscoverAdjacent(neighboorNode);
            _openSet.Add(neighboorNode.Node.Key);
        }

        public override void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            base.ExamineTraversalInfo(traversalNodeInfo);
            _openSet.Remove(traversalNodeInfo.CurrentNode.Key);
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
