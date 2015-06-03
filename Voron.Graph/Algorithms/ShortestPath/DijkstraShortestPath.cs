using System.Threading;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
	public class DijkstraShortestPath : BaseSingleDestinationShortestPath
    {
        public DijkstraShortestPath(Transaction tx,
            GraphStorage graphStorage,
            Node root,
            Node targetNode,
            CancellationToken cancelToken)
            : base(tx,graphStorage,root,targetNode,
            new SingleDestinationShortestPathVisitor(root, targetNode,
                (nodeFrom, nodeTo) => 0,
                (traversalInfo, adjacentNode) => adjacentNode.EdgeTo.Weight + traversalInfo.TotalEdgeWeightUpToNow),
                new TraversalAlgorithm(tx,
                graphStorage,
                root,
                TraversalType.BFS,
                cancelToken),
            cancelToken    
            )
        {
            _shortestPathVisitor.IsProcessingQueueEmpty = () => _traversal.ProcessingQueueCount == 0;
        }      
    }
}
