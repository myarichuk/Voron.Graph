using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Impl;
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

        public Task<Node> FindOneAndUpdate(Transaction tx, Func<JObject,bool> searchPredicate, Func<long,JObject,JObject> newDataFactory)
        {
            throw new NotImplementedException();
        }

        public Task<Node> FindOne(Transaction tx, Func<JObject,bool> searchPredicate)
        {
            return Task.Run(() =>
            {
                OnStateChange(AlgorithmState.Running);

                var visitedNodes = new HashSet<long>();
                var processingQueue = new Queue<Node>();
                Node foundNode = null;
                var rootNode = GetRootNode(tx);
                processingQueue.Enqueue(rootNode);

                while (processingQueue.Count > 0)
                {
                    AbortExecutionIfNeeded();

                    var currentNode = processingQueue.Dequeue();
                    visitedNodes.Add(currentNode.Key);
                    OnNodeVisited(currentNode);

                    if (searchPredicate(currentNode.Data))
                    {
                        foundNode = currentNode;
                        OnNodeFound(currentNode);
                        OnStateChange(AlgorithmState.Finished);
                        break;
                    }

                    foreach (var childNode in _graphStorage.Queries.GetAdjacentOf(tx, currentNode)
                                                                   .Where(node => !visitedNodes.Contains(node.Key)))
                    {
                        AbortExecutionIfNeeded();
                        processingQueue.Enqueue(childNode);                        
                    }

                }
                return foundNode;
            });
         
        }

        public Task<IEnumerable<Node>> FindMany(Transaction tx, Func<JObject, bool> searchPredicate)
        {
            var visitedNodes = new HashSet<long>();
            throw new NotImplementedException();
        }

        protected override Node GetRootNode(Transaction tx)
        {
            using(var iter = tx.NodeTree.Iterate(tx.VoronTransaction))
            {
                if (!iter.Seek(Slice.BeforeAllKeys))
                    return null;

                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(), resultStream.ToJObject());
            }
        }

        public event Action<Node> NodeVisited;

        protected void OnNodeVisited(Node node)
        {
            using(Lock())
            {
                var nodeVisited = NodeVisited;
                if (nodeVisited != null)
                    nodeVisited(node);
            }
        }

        public event Action<Node> NodeFound;

        protected void OnNodeFound(Node node)
        {
            using(Lock())
            {
                var nodeFound = NodeFound;
                if (nodeFound != null)
                    nodeFound(node);
            }
        }
    }
}
