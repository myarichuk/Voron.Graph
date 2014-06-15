using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public abstract class BaseSingleDestinationShortestPath : BaseAlgorithm, ISingleDestinationShortestPath
    {
        protected readonly TraversalAlgorithm _traversal;

        protected readonly SingleDestinationShortestPathVisitor _shortestPathVisitor;
        protected readonly Node _rootNode;
        protected readonly Node _targetNode;

        protected BaseSingleDestinationShortestPath(Transaction tx,
            GraphStorage graphStorage,
            Node root,
            Node targetNode,
            SingleDestinationShortestPathVisitor shortestPathVisitor,
            TraversalAlgorithm traversal,
            CancellationToken cancelToken)
        {
            _rootNode = root;
            _targetNode = targetNode;
            _shortestPathVisitor = shortestPathVisitor;
            _traversal = traversal;
            _traversal.Visitor = shortestPathVisitor;
        }

        public IEnumerable<long> Execute()
        {
            _traversal.Traverse();
            if (_shortestPathVisitor.HasDiscoveredDestination == false)
                return null;

            return GetShortestPathToNode(_shortestPathVisitor.PreviousNodeInOptimalPath, _targetNode);
        }

        public async Task<IEnumerable<long>> ExecuteAsync()
        {
            await _traversal.TraverseAsync();
            if (_shortestPathVisitor.HasDiscoveredDestination == false)
                return null;

            return GetShortestPathToNode(_shortestPathVisitor.PreviousNodeInOptimalPath, _targetNode);
        }

        private IEnumerable<long> GetShortestPathToNode(Dictionary<long, long> previousNodeInOptimalPath, Node targetNode)
        {
            var results = new Stack<long>();
            if (targetNode == null)
                throw new ArgumentNullException("node");

            if (!previousNodeInOptimalPath.ContainsKey(targetNode.Key))
                return results;

            long currentNodeKey = targetNode.Key;
            while (_rootNode.Key != currentNodeKey)
            {
                results.Push(currentNodeKey);
                currentNodeKey = previousNodeInOptimalPath[currentNodeKey];
                if (currentNodeKey == _rootNode.Key)
                    results.Push(currentNodeKey);
            }

            return results;
        }

    }
}
