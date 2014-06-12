using System;
using System.Collections.Generic;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Exceptions;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class DijkstraMultiDestinationShortestPathVisitor : MultiDestinationShortestPathVisitor
    {
        public DijkstraMultiDestinationShortestPathVisitor(Node rootNode,
            Func<Node, Node, double> h,
            Func<TraversalNodeInfo, NodeWithEdge, double> g)
            :base(rootNode,h,g)
        {

        }

        protected override void ValidateAdjacentNodeThrowIfNeeded(NodeWithEdge adjacentNode)
        {
            if (adjacentNode.EdgeTo.Weight < 0)
                throw new AlgorithmConstraintException(string.Format(@"Encountered a node with negative weight
                    between key = {0}, key = {1}. Dijkstra's algorithm for shortest path does not support edges with negative weights",
                        adjacentNode.EdgeTo.Key.NodeKeyFrom, adjacentNode.EdgeTo.Key.NodeKeyTo));
        }
    }
}
