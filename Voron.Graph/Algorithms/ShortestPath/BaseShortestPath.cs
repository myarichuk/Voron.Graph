using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public abstract class BaseShortestPath : BaseAlgorithm, ISingleSourceShortestPath
    {
        protected readonly IVisitor _visitor;
        protected readonly TraversalAlgorithm _traversal;
        protected readonly Node _rootNode;

        protected BaseShortestPath(Transaction tx,
            GraphStorage graphStorage, 
            Node root, 
            CancellationToken cancelToken,
            IVisitor visitor,
            INodeTraversalStore<TraversalNodeInfo> processingQueue)
        {
            _rootNode = root;
            _visitor = visitor;
            _traversal = new TraversalAlgorithm(tx, graphStorage, root,processingQueue, cancelToken)
            {
                Visitor = visitor
            };
        }

        public ISingleSourceShortestPathResults Execute()
        {
            throw new NotImplementedException();
        }

        public Task<ISingleSourceShortestPathResults> ExecuteAsync()
        {
            throw new NotImplementedException();
        }

        public class ShortestPathResults : ISingleSourceShortestPathResults
        {
            public Node RootNode { get; internal set; }
            public Dictionary<long, long> DistancesByNode { get; internal set; }
            public Dictionary<long, long> PreviousNodeInOptimalPath { get; internal set; }

            public ShortestPathResults()
            {
                DistancesByNode = new Dictionary<long, long>();
                PreviousNodeInOptimalPath = new Dictionary<long, long>();
            }

            public IEnumerable<long> GetShortestPathToNode(Node node)
            {
                var results = new Stack<long>();
                Debug.Assert(RootNode != null);
                if (node == null)
                    throw new ArgumentNullException("node");

                if (!PreviousNodeInOptimalPath.ContainsKey(node.Key))
                    return results;

                long currentNodeKey = node.Key;
                while (RootNode.Key != currentNodeKey)
                {
                    results.Push(currentNodeKey);
                    currentNodeKey = PreviousNodeInOptimalPath[currentNodeKey];
                    if (currentNodeKey == RootNode.Key)
                        results.Push(currentNodeKey);
                }

                return results;
            }
        }
    }
}
