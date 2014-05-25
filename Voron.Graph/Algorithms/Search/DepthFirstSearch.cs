using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Search
{
    public class DepthFirstSearch : BaseRootedAlgorithm, ISearchAlgorithm
    {

        private readonly GraphStorage _graphStorage;

        public DepthFirstSearch(GraphStorage graphStorage, CancellationToken cancelToken)
            : base(cancelToken)
        {
            _graphStorage = graphStorage;
        }

        protected override Node GetRootNode(Transaction tx)
        {
            using (var iter = tx.NodeTree.Iterate(tx.VoronTransaction))
            {
                if (!iter.Seek(Slice.BeforeAllKeys))
                    return null;

                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
                {
                    Etag etag;
                    JObject value;
                    Util.EtagAndValueFromStream(resultStream, out etag, out value);
                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag);
                }
            }
        }

        public Task Traverse(Transaction tx, Func<JObject, bool> searchPredicate, Func<bool> shouldStopPredicate, ushort? edgeTypeFilter = null)
        {
            if (State == AlgorithmState.Running)
                throw new InvalidOperationException("The search already running");
            else
                return Task.Run(() =>
                {
                    OnStateChange(AlgorithmState.Running);

                    var rootNode = GetRootNode(tx);
                    var visitedNodes = new HashSet<long>();
                    var processingQueue = new Stack<Node>();
                    processingQueue.Push(rootNode);

                    while (processingQueue.Count > 0)
                    {
                        if (shouldStopPredicate())
                        {
                            OnStateChange(AlgorithmState.Finished);
                            break;
                        }

                        AbortExecutionIfNeeded();

                        var currentNode = processingQueue.Pop();
                        if (searchPredicate(currentNode.Data))
                        {
                            OnNodeFound(currentNode);
                            if (shouldStopPredicate())
                            {
                                OnStateChange(AlgorithmState.Finished);
                                break;
                            }
                        }

                        if(!visitedNodes.Contains(currentNode.Key))
                        {
                            visitedNodes.Add(currentNode.Key);
                            OnNodeVisited(currentNode);

                            foreach (var node in _graphStorage.Queries.GetAdjacentOf(tx, currentNode, edgeTypeFilter ?? 0)
                                                                      .Where(node => !visitedNodes.Contains(node.Key)))
                            {
                                AbortExecutionIfNeeded();
                                processingQueue.Push(node);
                            }
                        }
                    }
                    OnStateChange(AlgorithmState.Finished);
                });
        }

        public event Action<Node> NodeVisited;

        protected void OnNodeVisited(Node node)
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
