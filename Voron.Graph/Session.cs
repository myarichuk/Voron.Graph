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
        private readonly Func<Tuple<long, long>> _requestIdRangeFunc;

        private long _currentId;
        private long _maxId;
        private readonly object _syncObject = new object();

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc, Func<Tuple<long, long>> requestIdRangeFunc)
        {
            _snapshot = snapshot;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _writerFunc = writerFunc;
            _writeBatch = new WriteBatch();
            _objectsToDispose = new ConcurrentDictionary<long, IDisposable>();
            _requestIdRangeFunc = requestIdRangeFunc;
        }        

        //TODO: refactor all GetNextId related stuff
        //this is a hack and needs to be refactored
        private void RequestIdRange()
        {
            var idRange = _requestIdRangeFunc();
            _currentId = idRange.Item1;
            _maxId = idRange.Item2;

            Debug.Assert(_maxId > _currentId);
        }

        private long GetNextId()
        {
            if (_currentId + 1 >= _maxId)
                RequestIdRange();

            return ++_currentId;
        }

        internal string NodeTreeName
        {
            get { return _nodeTreeName; }
        }

        internal string EdgeTreeName
        {
            get { return _edgeTreeName; }
        }

        internal string DisconnectedNodesTreeName
        {
            get { return _disconnectedNodesTreeName; }
        }

        internal WriteBatch WriteBatch
        {
            get { return _writeBatch; }
        }

        internal SnapshotReader Snapshot
        {
            get { return _snapshot; }
        }      

        public Iterator<Node> IterateNodes()
        {
            var iterator = _snapshot.Iterate(_nodeTreeName, _writeBatch);

            return new Iterator<Node>(iterator,
                (key, value) =>
                {
                    value.Position = 0;
                    return new Node(key.ToInt64(), value);
                });
        }

        public Iterator<Edge> IterateEdges()
        {
            var iterator = _snapshot.Iterate(_edgeTreeName, _writeBatch);

            return new Iterator<Edge>(iterator,
                (key, value) =>
                {
                    value.Position = 0;

                    var currentKey = key.ToEdgeTreeKey();
                    return new Edge(currentKey.NodeKeyFrom, currentKey.NodeKeyTo, value);
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


        public Node CreateNode(Stream value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var key = GetNextId();
            var nodeKey = key.ToSlice();

            _writeBatch.Add(nodeKey, value, _nodeTreeName);
            _writeBatch.Add(nodeKey, Stream.Null, _disconnectedNodesTreeName);

            return new Node(key, value);
        }

        public Edge CreateEdge(Node nodeFrom, Node nodeTo, Stream value = null)
        {
            if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (nodeTo == null) throw new ArgumentNullException("nodeTo");

            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value);
            var test = edge.Key.ToSlice();
            var rtest = test.ToEdgeTreeKey();
            _writeBatch.Add(edge.Key.ToSlice(), value ?? Stream.Null, _edgeTreeName);

            _writeBatch.Delete(nodeFrom.Key.ToSlice(), _disconnectedNodesTreeName);
            _writeBatch.Delete(nodeTo.Key.ToSlice(), _disconnectedNodesTreeName);
            
            return edge;
        }

        public void Delete(Node node)
        {
            throw new NotImplementedException();
        }

        public void Delete(Edge edge)
        {
            throw new NotImplementedException();
        }

        public Stream GetValueOf(Node node)
        {
            throw new NotImplementedException();
        }

        public Stream GetValueOf(Edge edge)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Node> GetAdjacentOf(Node node)
        {
            throw new NotImplementedException();
        }

        public bool IsIsolated(Node node)
        {
            throw new NotImplementedException();
        }
    }
}