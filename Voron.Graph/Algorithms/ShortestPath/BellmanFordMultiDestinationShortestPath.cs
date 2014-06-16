using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using Voron.Graph.Exceptions;
using Voron.Graph.Impl;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public class BellmanFordMultiDestinationShortestPath : BaseAlgorithm, IMultiDestinationShortestPath
    {
        private readonly Node _rootNode;
        private readonly GraphAdminQueries _graphAdminQueries;
        private readonly Transaction _tx;
        private readonly CancellationToken _cancelToken;

        public BellmanFordMultiDestinationShortestPath(Transaction tx, GraphStorage graphStorage, Node root, CancellationToken cancelToken)
        {
            _graphAdminQueries = graphStorage.AdminQueries;
            _rootNode = root;
            _tx = tx;
            _cancelToken = cancelToken;
        }

        public IMultiDestinationShortestPathResults Execute()
        {
            var getAllEdgesTask = _graphAdminQueries.GetAllEdges(_tx, _cancelToken);
            getAllEdgesTask.Wait();
            var edges = getAllEdgesTask.Result;

            var results = ExecuteAlgorithm(edges);
            return results;
        }

        public async Task<IMultiDestinationShortestPathResults> ExecuteAsync()
        {
            var edges = await _graphAdminQueries.GetAllEdges(_tx, _cancelToken);

            var results = ExecuteAlgorithm(edges);
            return results;
        }

        private ShortestPathResults ExecuteAlgorithm(List<Edge> edges)
        {
            var results = new ShortestPathResults(_rootNode);

            results.WeightsByNodeKey.Add(_rootNode.Key, 0);

            for (long i = 1; i < _tx.NodeCount - 1; i++)
            {
                bool hasMadeChange = false;
                edges.ForEach(edge =>
                {
                    if (!results.WeightsByNodeKey.ContainsKey(edge.Key.NodeKeyFrom))
                    {
                        results.WeightsByNodeKey.Add(edge.Key.NodeKeyFrom, long.MaxValue);
                        hasMadeChange = true;
                    }
                    if (!results.WeightsByNodeKey.ContainsKey(edge.Key.NodeKeyTo))
                    {
                        results.WeightsByNodeKey.Add(edge.Key.NodeKeyTo, long.MaxValue);
                        hasMadeChange = true;
                    }
                    if (results.WeightsByNodeKey[edge.Key.NodeKeyFrom] + edge.Weight < results.WeightsByNodeKey[edge.Key.NodeKeyTo])
                    {
                        results.WeightsByNodeKey[edge.Key.NodeKeyTo] = results.WeightsByNodeKey[edge.Key.NodeKeyFrom] + edge.Weight;
                        results.PreviousNodeInOptimizedPath[edge.Key.NodeKeyTo] = edge.Key.NodeKeyFrom;
                        hasMadeChange = true;
                    }
                });

                if (hasMadeChange)
                    break;
            }

            //detect all negative cycles
            edges.ForEach(edge =>
            {
                if (results.WeightsByNodeKey[edge.Key.NodeKeyFrom] + edge.Weight < results.WeightsByNodeKey[edge.Key.NodeKeyTo])
                    throw new AlgorithmConstraintException("Bellman-Ford algorithm cannot handle negative weight cycle.");
            });
            return results;
        }

        public class ShortestPathResults : IMultiDestinationShortestPathResults
        {
            public Node RootNode { get; internal set; }
            public Dictionary<long, long> WeightsByNodeKey { get; internal set; }
            public Dictionary<long, long> PreviousNodeInOptimizedPath { get; internal set; }

            public ShortestPathResults(Node rootNode)
            {
                RootNode = rootNode;
                WeightsByNodeKey = new Dictionary<long, long>();
                PreviousNodeInOptimizedPath = new Dictionary<long, long>();
            }

            public IEnumerable<long> GetShortestPathToNode(Node node)
            {
                var results = new Stack<long>();
                Debug.Assert(RootNode != null);

                if (node == null)
                    throw new ArgumentNullException("node");

                if (!PreviousNodeInOptimizedPath.ContainsKey(node.Key))
                    return null;

                long currentNodeKey = node.Key;
                while (RootNode.Key != currentNodeKey)
                {
                    results.Push(currentNodeKey);
                    currentNodeKey = PreviousNodeInOptimizedPath[currentNodeKey];
                    if (currentNodeKey == RootNode.Key)
                        results.Push(currentNodeKey);
                }

                return results;               
            }
        }
    }
}
