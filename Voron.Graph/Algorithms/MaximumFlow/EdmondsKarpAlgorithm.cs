using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.MaximumFlow
{
    public class EdmondsKarpAlgorithm : BaseMaximumFlow
    {
        private readonly Node _sourceNode;
        private readonly Node _targetNode;
        private readonly GraphStorage _storage;
        private readonly Dictionary<Tuple<long,long>, long> _flow;
        private readonly Transaction _tx;
       
        public EdmondsKarpAlgorithm(Transaction tx, 
            GraphStorage graphStorage, 
            Node sourceNode, 
            Node targetNode,
            Func<Edge, long> capacity,
            CancellationToken? cancelToken = null)
            : base(capacity)
        {
            _sourceNode = sourceNode;
            _targetNode = targetNode;
            _storage = graphStorage;
            _flow = new Dictionary<Tuple<long, long>, long>();
            _tx = tx;
        }

        public override long MaximumFlow(long startNodeKey, long endNodeKey)
        {
            OnStateChange(AlgorithmState.Running);
            long maximumFlow = 0;
            EdmondsKarpBFSVisitor flowPathVisitor;
            do
            {
                flowPathVisitor = new EdmondsKarpBFSVisitor(_sourceNode, _targetNode, _capacity, _flow);
                new TraversalAlgorithm(_tx, _storage, _sourceNode, TraversalType.BFS, null)
                {
                    Visitor = flowPathVisitor
                }.Traverse();
                
                if(flowPathVisitor.HasPath)
                {
                    maximumFlow += flowPathVisitor.BottleneckCapacity;
                }

            } while (flowPathVisitor.HasPath);

            OnStateChange(AlgorithmState.Finished);
            return maximumFlow;
        }

        public override Task<long> MaximumFlowAsync(long startNodeKey, long endNodeKey)
        {
            throw new NotImplementedException();
        }

        private class EdmondsKarpBFSVisitor : IVisitor
        {
            private readonly Dictionary<long,long> _previousNodeInPath;
            private readonly Dictionary<Tuple<long,long>, long> _pathCapacity;
            private readonly Dictionary<Tuple<long, long>, long> _flow;

            private readonly Func<Edge, long> _capacity;
            private readonly Node _sourceNode;
            private readonly Node _targetNode;
            private TraversalNodeInfo _currentTraversalNodeInfo;
            private bool _hasDiscoveredDestination;

            public EdmondsKarpBFSVisitor(Node sourceNode, Node targetNode, Func<Edge, long> capacity, Dictionary<Tuple<long, long>, long> flow)
            {
                _capacity = capacity;
                _previousNodeInPath = new Dictionary<long, long>();
                _pathCapacity = new Dictionary<Tuple<long, long>, long>();
                _sourceNode = sourceNode;
                _targetNode = targetNode;
                _flow = flow;
                _hasDiscoveredDestination = false;
            }

            public void DiscoverAdjacent(Primitives.NodeWithEdge neighboorNode)
            {
            }

            public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
            {
                _currentTraversalNodeInfo = traversalNodeInfo;
                _previousNodeInPath[traversalNodeInfo.CurrentNode.Key] = traversalNodeInfo.ParentNode.Key;

                if (_targetNode != null && _currentTraversalNodeInfo.CurrentNode.Key == _targetNode.Key)
                {
                    UpdateFlowCosts();
                    _hasDiscoveredDestination = true;
                }
            }

            private void UpdateFlowCosts()
            {
                var currentKey = _targetNode.Key;
                do
                {
                    var capacityKey = Tuple.Create(_previousNodeInPath[currentKey], currentKey);
                    _flow[capacityKey] += _pathCapacity[capacityKey];

                    currentKey = _previousNodeInPath[currentKey];
                } while (currentKey != _sourceNode.Key);
                
            }

            public bool ShouldSkipAdjacentNode(Primitives.NodeWithEdge adjacentNode)
            {
                var key = EdgeKeyToTuple(adjacentNode.EdgeTo.Key);
                if (!_flow.ContainsKey(key))
                    _flow[key] = 0;

                var capacity = _capacity(adjacentNode.EdgeTo);
                _pathCapacity[Tuple.Create(adjacentNode.EdgeTo.Key.NodeKeyFrom,adjacentNode.EdgeTo.Key.NodeKeyTo)] = capacity;

                return _previousNodeInPath.ContainsKey(adjacentNode.Node.Key) ||
                       capacity - _flow[key] <= 0;
            }

            public bool HasPath
            {
                get
                {
                    return _hasDiscoveredDestination;
                }
            }

            public bool ShouldStopTraversal
            {
                get { return _hasDiscoveredDestination; }
            }

            public IEnumerable<long> FlowPath
            {
                get
                {
                    var currentKey = _targetNode.Key;
                    do
                    {                        
                        currentKey = _previousNodeInPath[currentKey];
                        yield return currentKey;
                    } while (currentKey != _sourceNode.Key);
                }
            }

            public long BottleneckCapacity
            {
                get
                {
                    if (!HasPath)
                        throw new InvalidOperationException("When there is no path, bottleneck capacity is irrelevant");

                    var currentKey = _targetNode.Key;
                    var minimalCapacityOnPath = long.MaxValue;
                    do
                    {
                        var edgeCapacity = _pathCapacity[Tuple.Create(_previousNodeInPath[currentKey], currentKey)];
                        minimalCapacityOnPath = Math.Min(minimalCapacityOnPath, edgeCapacity);
                        currentKey = _previousNodeInPath[currentKey];
                    } while (currentKey != _sourceNode.Key);

                    return minimalCapacityOnPath;
                }
            }

            private Tuple<long,long> EdgeKeyToTuple(EdgeTreeKey key)
            {
                return Tuple.Create(key.NodeKeyFrom, key.NodeKeyTo);
            }
        }
    }
}
