using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Exceptions;

namespace Voron.Graph.Algorithms.ShortestPath
{
	public class BellmanFordMultiDestinationShortestPath : BaseAlgorithm, IMultiDestinationShortestPath
    {
        private readonly Node _rootNode;
        private readonly GraphStorage.GraphAdmin _graphAdmin;
        private readonly Transaction _tx;
        private readonly CancellationToken _cancelToken;

        public BellmanFordMultiDestinationShortestPath(Transaction tx, GraphStorage graphStorage, Node root, CancellationToken cancelToken)
        {
            _graphAdmin = graphStorage.Admin;
            _rootNode = root;
            _tx = tx;
            _cancelToken = cancelToken;
        }

        public IMultiDestinationShortestPathResults Execute()
        {
            var edges = _graphAdmin.GetAllEdges(_tx).ToList();

            var results = ExecuteAlgorithm(edges);
            return results;
        }

        public Task<IMultiDestinationShortestPathResults> ExecuteAsync()
        {
			return Task.Run(() => Execute());
        }

        private ShortestPathResults ExecuteAlgorithm(List<Edge> edges)
        {
            var results = new ShortestPathResults(_rootNode);

            results.WeightsByNodeKey.Add(_rootNode.Key, 0);

            for (long i = 1; i < _tx.NodeCount - 1; i++)
            {
                var hasMadeChange = false;
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
            internal Dictionary<long, long> WeightsByNodeKey { get; set; }
            internal Dictionary<long, long> PreviousNodeInOptimizedPath { get; set; }

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

                var currentNodeKey = node.Key;
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
