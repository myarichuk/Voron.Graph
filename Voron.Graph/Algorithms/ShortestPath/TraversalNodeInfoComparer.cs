using System;
using System.Collections.Generic;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class TraversalNodeInfoComparer : IComparer<TraversalNodeInfo>
    {
        private Func<Node, Node, double> _heuristic;
        private Node _rootNode;
        public TraversalNodeInfoComparer(Node rootNode, Func<Node, Node, double> heuristic)
        {
            _heuristic = heuristic;
            _rootNode = rootNode;
        }

        public int Compare(TraversalNodeInfo first, TraversalNodeInfo second)
        {
            var firstScore = _heuristic(first.CurrentNode, _rootNode);
            var secondScore = _heuristic(second.CurrentNode, _rootNode);
            
            if (firstScore == secondScore)
                return 0;

            return (firstScore < secondScore) ? 1 : -1;
        }
    }

}
