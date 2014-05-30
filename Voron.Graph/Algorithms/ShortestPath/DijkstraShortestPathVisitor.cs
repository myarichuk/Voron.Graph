using System.Collections.Generic;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Exceptions;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class DijkstraShortestPathVisitor : IVisitor
    {
        public Dictionary<long, long> DistancesByNode;
        public Dictionary<long, long> PreviousNodeInOptimalPath;

        private TraversalNodeInfo currentTraversalNodeInfo;

        public DijkstraShortestPathVisitor()
        {
            DistancesByNode = new Dictionary<long, long>();
            PreviousNodeInOptimalPath = new Dictionary<long, long>();
        }

        public void DiscoverAdjacent(NodeWithEdge neighboorNode)
        {
            if (neighboorNode.EdgeTo.Weight < 0)
                throw new AlgorithmConstraintException(string.Format(@"Encountered a node with negative weight
                    between key = {0}, key = {1}. Dijkstra's algorithm for shortest path does not support edges with negative weights", 
                        neighboorNode.EdgeTo.Key.NodeKeyFrom, neighboorNode.EdgeTo.Key.NodeKeyTo));
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

            if (updateOptimalPath)
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
