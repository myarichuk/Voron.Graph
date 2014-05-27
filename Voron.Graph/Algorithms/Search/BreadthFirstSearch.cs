using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Voron.Graph.Algorithms.Search
{
    public class BreadthFirstSearch : BaseRootedAlgorithm, ISearchAlgorithm
    {
        private readonly GraphStorage _graphStorage;

        public BreadthFirstSearch(GraphStorage graphStorage, CancellationToken cancelToken)
            : base(cancelToken)
        {
            _graphStorage = graphStorage;
        }

        public Task Traverse(Transaction tx,
            Func<JObject, bool> searchPredicate,
            Func<bool> shouldStopPredicate,
            Node rootNode = null,
            ushort? edgeTypeFilter = null,
            uint? traverseDepthLimit = null)
        {
	        if (State == AlgorithmState.Running)
                throw new InvalidOperationException("The search already running");

	        return Task.Run(() =>
	        {
		        OnStateChange(AlgorithmState.Running);

		        var visitedNodes = new HashSet<long>();
		        var processingQueue = new Queue<NodeVisitedEventArgs>();
		        rootNode = rootNode ?? GetDefaultRootNode(tx);
		        processingQueue.Enqueue(new NodeVisitedEventArgs(rootNode,null, 1));

		        while (processingQueue.Count > 0)
		        {
			        if (shouldStopPredicate())
			        {
				        OnStateChange(AlgorithmState.Finished);
				        break;
			        }
    
			        AbortExecutionIfNeeded();
    
			        var currentVisitedEventInfo = processingQueue.Dequeue();
			        visitedNodes.Add(currentVisitedEventInfo.VisitedNode.Key);
			        OnNodeVisited(currentVisitedEventInfo);

			        if (searchPredicate(currentVisitedEventInfo.VisitedNode.Data))
			        {
				        OnNodeFound(currentVisitedEventInfo.VisitedNode);
				        if (shouldStopPredicate() ||
                            (traverseDepthLimit.HasValue && currentVisitedEventInfo.TraversedEdgeCount >= traverseDepthLimit.Value))
				        {
					        OnStateChange(AlgorithmState.Finished);
					        break;
				        }
			        }

			        foreach (var childNode in _graphStorage.Queries.GetAdjacentOf(tx, currentVisitedEventInfo.VisitedNode, edgeTypeFilter ?? 0)
				        .Where(node => !visitedNodes.Contains(node.Key)))
			        {
				        AbortExecutionIfNeeded();
                        processingQueue.Enqueue(new NodeVisitedEventArgs(childNode, currentVisitedEventInfo.VisitedNode, currentVisitedEventInfo.TraversedEdgeCount + 1));
			        }
    
		        }

		        OnStateChange(AlgorithmState.Finished);
	        });
        }

	    protected override Node GetDefaultRootNode(Transaction tx)
        {
            using(var iter = tx.NodeTree.Iterate())
            {
                if (!iter.Seek(Slice.BeforeAllKeys))
                    return null;

                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
                {
                    Etag etag;
                    JObject value;
                    Util.EtagAndValueFromStream(resultStream,out etag,out value);
                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(),value,etag);
                }
            }
        }

        public event Action<NodeVisitedEventArgs> NodeVisited;

        protected void OnNodeVisited(NodeVisitedEventArgs node)
        {
            var nodeVisited = NodeVisited;
            if (nodeVisited != null)
                nodeVisited(node);
        }

        public event Action<Node> NodeFound;

        protected void OnNodeFound(Node node)
        {
            var nodeFound = NodeFound;
            if (nodeFound != null)
                nodeFound(node);
        }
    }
}
