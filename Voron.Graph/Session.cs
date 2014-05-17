using System;
using System.Collections.Generic;
using System.IO;
using Voron.Impl;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using Voron.Util.Conversion;
using System.Runtime.InteropServices;
using Voron.Graph.Interfaces;

namespace Voron.Graph
{
    public class Session : ISession
    {
        private WriteBatch _writeBatch;
        private SnapshotReader _snapshot;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly Action<WriteBatch> _writerFunc;
        private readonly string _disconnectedNodesTreeName;
        private ConcurrentDictionary<long, IDisposable> _objectsToDispose;
        private readonly Conventions _conventions;
        private readonly object _syncObject = new object();
        private readonly ConcurrentBag<IDisposable> _disposalList;
        private readonly IIdGenerator _idGenerator;

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc,Conventions conventions)
        {
            _snapshot = snapshot;
            _conventions = conventions;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _writerFunc = writerFunc;
            _writeBatch = new WriteBatch();
            _objectsToDispose = new ConcurrentDictionary<long, IDisposable>();
            _disposalList = new ConcurrentBag<IDisposable>();
            _idGenerator = _conventions.IdGeneratorFactory();
        }              

        public Iterator<Node> IterateNodes()
        {
            var iterator = _snapshot.Iterate(_nodeTreeName, _writeBatch);

            return new Iterator<Node>(iterator,
                (key, value) =>
                {
                    using (value)
                    {
                        value.Position = 0;
                        var node = new Node(key.ToInt64(), value, makeValueCopy: true);

                        _disposalList.Add(node);
                        return node;
                    }
                });
        }

        public Iterator<Edge> IterateEdges()
        {
            var iterator = _snapshot.Iterate(_edgeTreeName, _writeBatch);

            return new Iterator<Edge>(iterator,
                (key, value) =>
                {
                    using (value)
                    {
                        value.Position = 0;

                        var currentKey = key.ToEdgeTreeKey();
                        var edge = new Edge(currentKey.NodeKeyFrom, currentKey.NodeKeyTo, value, makeValueCopy: true);
                        
                        _disposalList.Add(edge);
                        return edge;
                    }
                });
        }

    

        public void SaveChanges()
        {
            _writerFunc(_writeBatch);
            _writeBatch.Dispose();
            _writeBatch = new WriteBatch();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool isDisposing)
        {           
            if (_snapshot != null)
            {
                _snapshot.Dispose();
                _snapshot = null;
            }

            foreach (var disposable in _disposalList)
                disposable.Dispose();

            if(isDisposing)
                GC.SuppressFinalize(this);
        }     

        ~Session()
        {
#if DEBUG
            if(_snapshot != null)
                Trace.WriteLine("Disposal for Session object was not called, disposing from finalizer. Stack Trace: " + new StackTrace());
#endif
            Dispose(false);
        }

        //in order to update, use existing key
        public Node CreateNode(Stream value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var key = _idGenerator.NextId();

            var nodeKey = key.ToSlice();

            _writeBatch.Add(nodeKey, value, _nodeTreeName);
            _writeBatch.Add(nodeKey, Stream.Null, _disconnectedNodesTreeName);

            return new Node(key, value);
        }

        public Edge CreateEdgeBetween(Node nodeFrom, Node nodeTo, Stream value = null)
        {
            if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (nodeTo == null) throw new ArgumentNullException("nodeTo");

            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value);
            _writeBatch.Add(edge.Key.ToSlice(), value ?? Stream.Null, _edgeTreeName);

            _writeBatch.Delete(nodeFrom.Key.ToSlice(), _disconnectedNodesTreeName);
            _writeBatch.Delete(nodeTo.Key.ToSlice(), _disconnectedNodesTreeName);
            
            return edge;
        }

        public void Delete(Node node)
        {
            var nodeKey = node.Key.ToSlice();
            _writeBatch.Delete(nodeKey, _nodeTreeName);
            _writeBatch.Delete(nodeKey, _disconnectedNodesTreeName); //just in case, doesn't have to be here
        }

        public void Delete(Edge edge)
        {
            var edgeKey = edge.Key.ToSlice();
            _writeBatch.Delete(edgeKey, _edgeTreeName);
        }

        public IEnumerable<Node> GetAdjacentOf(Node node, Func<ushort,bool> edgeTypePredicate = null)
        {
            var alreadyRetrievedKeys = new HashSet<long>();
            using (var edgeIterator = _snapshot.Iterate(_edgeTreeName, _writeBatch))
            {                
                edgeIterator.RequiredPrefix = node.Key.ToSlice();
                if (!edgeIterator.Seek(Slice.BeforeAllKeys))
                    yield break;

                do
                {
                    var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (edgeTypePredicate != null && edgeTypePredicate(edgeKey.Type) == false)
                        continue;

                    if(!alreadyRetrievedKeys.Contains(edgeKey.NodeKeyTo))
                    {                        
                        alreadyRetrievedKeys.Add(edgeKey.NodeKeyTo);
                        yield return NodeByKey(edgeKey.NodeKeyTo);
                    }

                } while (edgeIterator.MoveNext());
            }
        }

        public bool IsIsolated(Node node)
        {
            using (var edgeIterator = _snapshot.Iterate(_edgeTreeName, _writeBatch))
            {
                edgeIterator.RequiredPrefix = node.Key.ToSlice();
                return edgeIterator.Seek(Slice.BeforeAllKeys);
            }
        }


        public Node NodeByKey(long nodeKey)
        {
            var readResult = _snapshot.Read(_nodeTreeName, nodeKey.ToSlice(),_writeBatch);
            return new Node(nodeKey, readResult.Reader.AsStream());
        }

        
        public IEnumerable<Edge> GetEdgesBetween(Node nodeFrom, Node nodeTo,Func<ushort,bool> typePredicate = null)
        {
            using (var edgeIterator = _snapshot.Iterate(_edgeTreeName, _writeBatch))
            {
                edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);

                if (!edgeIterator.Seek(Slice.BeforeAllKeys))
                    yield break;

                do
                {
                    var edgeTreeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (typePredicate != null && !typePredicate(edgeTreeKey.Type))
                        continue;
                    var valueReader = edgeIterator.CreateReaderForCurrent();
                    Stream value = Stream.Null;
                    if (valueReader != null)
                        value = valueReader.AsStream();

                    yield return new Edge(edgeTreeKey, value);
                } while (edgeIterator.MoveNext());
            }
        }

       
    }
}