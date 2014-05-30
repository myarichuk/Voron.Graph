using System.Collections.Generic;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class ShortestPathVisitor : IVisitor
    {
        public Dictionary<long, long> DistancesByNode;
        public Dictionary<long, long> PreviousNodeInOptimalPath;

        private TraversalNodeInfo currentTraversalNodeInfo;

        public ShortestPathVisitor()
        {
            DistancesByNode = new Dictionary<long, long>();
            PreviousNodeInOptimalPath = new Dictionary<long, long>();
        }

        public void DiscoverAdjacent(NodeWithEdge neighboorNode)
        {
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
