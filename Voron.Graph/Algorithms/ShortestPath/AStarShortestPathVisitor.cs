
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
        
        private readonly HashSet<long> _openList;
        private readonly HashSet<long> _closedList;

        private TraversalNodeInfo _currentTraversalNodeInfo;

        public AStarShortestPathVisitor(long startNodeKey,Func<TraversalNodeInfo,bool> heuristic)
        {
            _openList = new HashSet<long>();
            _closedList = new HashSet<long>();

            _hasDiscoveredDestination = false;
        }

        public void DiscoverAdjacent(Primitives.NodeWithEdge neighboorNode)
        {

        }

        public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            _closedList.Add(traversalNodeInfo.CurrentNode.Key);
            _openList.Remove(traversalNodeInfo.CurrentNode.Key);
        }

        public bool ShouldStopTraversal
        {
            get 
            {
                return _hasDiscoveredDestination;
            }
        }

        public bool ShouldSkipAdjacentNode(Primitives.NodeWithEdge adjacentNode)
        {
            return _closedList.Contains(adjacentNode.Node.Key);
        }
    }
}
