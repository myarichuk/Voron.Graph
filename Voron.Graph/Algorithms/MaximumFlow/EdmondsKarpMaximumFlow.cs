using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.MaximumFlow
{
    //implementation of Ford–Fulkerson algorithm
    public class EdmondsKarpMaximumFlow : BaseMaximumFlow
    {
        private readonly Node _sourceNode;
        private readonly Node _targetNode;
        private readonly GraphStorage _storage;
        private readonly Dictionary<Tuple<long,long>, long> _flow;
        private readonly Transaction _tx;
        private readonly CancellationToken? _cancelToken;

        public EdmondsKarpMaximumFlow(Transaction tx, 
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
            _cancelToken = cancelToken;
            _flow = new Dictionary<Tuple<long, long>, long>();
            _tx = tx;
        }

        public override long MaximumFlow()
        {
            OnStateChange(AlgorithmState.Running);
            var algorithmTask = ExecuteAlgorithmAsync();
            algorithmTask.Wait();

            OnStateChange(AlgorithmState.Finished);
            return algorithmTask.Result;
        }        

        public override async Task<long> MaximumFlowAsync()
        {
            OnStateChange(AlgorithmState.Running);
            var maximumFlow = await ExecuteAlgorithmAsync();

            OnStateChange(AlgorithmState.Finished);
            return maximumFlow;
        }

        private async Task<long> ExecuteAlgorithmAsync()
        {
            long maximumFlow = 0;
            EdmondsKarpBFSVisitor flowPathVisitor;
            do
            {
                flowPathVisitor = new EdmondsKarpBFSVisitor(_sourceNode, _targetNode, _capacity, _flow);
                await new TraversalAlgorithm(_tx, _storage, _sourceNode, TraversalType.BFS, null)
                {
                    Visitor = flowPathVisitor
                }.TraverseAsync();

                if (flowPathVisitor.FoundPath)
                {
                    maximumFlow += flowPathVisitor.BottleneckCapacity;
                }

            } while (flowPathVisitor.FoundPath);
            return maximumFlow;
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

                if (traversalNodeInfo.ParentNode != null)
                {
                    _previousNodeInPath[traversalNodeInfo.CurrentNode.Key] = traversalNodeInfo.ParentNode.Key;

                    if (_targetNode != null && _currentTraversalNodeInfo.CurrentNode.Key == _targetNode.Key)
                    {
                        UpdateFlowCosts();
                        _hasDiscoveredDestination = true;
                    }
                }
            }

            private void UpdateFlowCosts()
            {
                var currentKey = _targetNode.Key;
                long bottleneckCapacity = long.MaxValue; 
                do
                {
                    var capacityKey = Tuple.Create(_previousNodeInPath[currentKey], currentKey);
                    bottleneckCapacity = Math.Min(bottleneckCapacity, _pathCapacity[capacityKey]);

                    currentKey = _previousNodeInPath[currentKey];
                } while (currentKey != _sourceNode.Key);

                currentKey = _targetNode.Key;
                do
                {
                    var capacityKey = Tuple.Create(_previousNodeInPath[currentKey], currentKey);
                    _flow[capacityKey] += bottleneckCapacity;

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

            public bool FoundPath
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
                    if (!FoundPath)
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
