using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class AStarShortestPath : BaseSingleDestinationShortestPath
    {
        public AStarShortestPath(Transaction tx, 
            GraphStorage graphStorage, 
            Node root, 
            Node targetNode,
            Func<Node,Node,double> heuristic,
            CancellationToken cancelToken)
            :base(tx,graphStorage,root,targetNode,
            new AStarShortestPathVisitor(root, targetNode,
                heuristic,
                (traversalInfo, adjacentNode) => adjacentNode.EdgeTo.Weight + traversalInfo.TotalEdgeWeightUpToNow),
            new TraversalAlgorithm(tx,
                graphStorage,
                root,
                new PriorityQueueTraversalStore(new TraversalNodeInfoComparer(root, heuristic)),
                cancelToken),
            cancelToken)
        {
            _shortestPathVisitor.IsProcessingQueueEmpty = () => _traversal.ProcessingQueueCount == 0;
        }      
    }
}
