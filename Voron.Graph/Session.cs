using System;
using System.Collections.Generic;
using System.IO;
using Voron.Impl;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;

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
        private ConcurrentDictionary<string,IDisposable> _objectsToDispose;

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc)
        {
            _snapshot = snapshot;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _writerFunc = writerFunc;
            _writeBatch = new WriteBatch();
            _objectsToDispose = new ConcurrentDictionary<string, IDisposable>();
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

        public IEnumerable<Node> Nodes
        {
            get
            {
                using(var iterator = _snapshot.Iterate(_nodeTreeName))
                {
                    if (!iterator.Seek(Slice.BeforeAllKeys))
                        yield break;

                    do
                    {
                        var data = iterator.CreateReaderForCurrent().AsStream();
                        data.Position = 0;
                        var currentKey = iterator.CurrentKey.ToString();
                        var node = new Node(currentKey, data);

                        _objectsToDispose.AddOrUpdate(currentKey, node, (key, existingDisposable) =>
                            {
                                if (existingDisposable != null)
                                    existingDisposable.Dispose();
                                return node;
                            });

                        yield return node;
                    } while (iterator.MoveNext());
                }
            }
        }

        public Iterator<Node> IterateNodes()
        {
            var iterator = _snapshot.Iterate(_nodeTreeName, _writeBatch);

            return new Iterator<Node>(iterator,
                (key, value) =>
                {
                    value.Position = 0;
                    return new Node(key.ToString(), value);
                });
        }

        public Iterator<Edge> IterateEdges()
        {
            var iterator = _snapshot.Iterate(_edgeTreeName, _writeBatch);

            return new Iterator<Edge>(iterator,
                (key, value) =>
                {
                    value.Position = 0;
                    var currentKey = Util.ParseEdgeTreeKey(key.ToString());
                    return new Edge(currentKey.NodeKeyFrom, currentKey.NodeKeyTo, value);
                });
        }

        public void PutNode(string nodeKey, Stream value)
        {
            if (String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            _writeBatch.Add(nodeKey, value, _nodeTreeName);
            _writeBatch.Add(nodeKey, Stream.Null,_disconnectedNodesTreeName);
        }

        public void PutEdge(string nodeKeyFrom, string nodeKeyTo, Stream value = null)
        {
            if (String.IsNullOrWhiteSpace(nodeKeyFrom)) throw new ArgumentNullException("nodeKeyFrom");
            if (String.IsNullOrWhiteSpace(nodeKeyTo)) throw new ArgumentNullException("nodeKeyTo");

            _writeBatch.Add(Util.CreateEdgeTreeKey(nodeKeyFrom, nodeKeyTo), value ?? Stream.Null, _edgeTreeName);

            _writeBatch.Delete(nodeKeyFrom, _disconnectedNodesTreeName);
            _writeBatch.Delete(nodeKeyTo, _disconnectedNodesTreeName);
        }

        public void DeleteNode(string nodeKey)
        {
            if (String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            _writeBatch.Delete(nodeKey, _nodeTreeName);

            ushort? version;

            if(_snapshot.Contains(_disconnectedNodesTreeName,nodeKey,out version,_writeBatch))
                _writeBatch.Delete(nodeKey, _disconnectedNodesTreeName);
        }

        public void DeleteEdge(string nodeKeyFrom, string nodeKeyTo)
        {
            if (String.IsNullOrWhiteSpace(nodeKeyFrom)) throw new ArgumentNullException("nodeKeyFrom");
            if (String.IsNullOrWhiteSpace(nodeKeyTo)) throw new ArgumentNullException("nodeKeyTo");

            _writeBatch.Delete(Util.CreateEdgeTreeKey(nodeKeyFrom, nodeKeyTo), _edgeTreeName);
            if (IsIsolated(nodeKeyFrom))
                _writeBatch.Add(nodeKeyFrom,Stream.Null,_disconnectedNodesTreeName);
        }

        public bool IsIsolated(string nodeKey)
        {
            using(var iterator = _snapshot.Iterate(_edgeTreeName))
            {
                iterator.RequiredPrefix = nodeKey;
                return iterator.Seek(Slice.BeforeAllKeys);
            }
        }

        public Stream GetNode(string nodeKey)
        {
            if(String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            var readResult = _snapshot.Read(_nodeTreeName, nodeKey, _writeBatch);
            return readResult.Reader.AsStream();
        }

        public Stream GetEdge(string nodeKeyFrom, string nodeKeyTo)
        {
            if (String.IsNullOrWhiteSpace(nodeKeyFrom)) throw new ArgumentNullException("nodeKeyFrom");
            if (String.IsNullOrWhiteSpace(nodeKeyTo)) throw new ArgumentNullException("nodeKeyTo");

            var readResult = _snapshot.Read(_edgeTreeName, Util.CreateEdgeTreeKey(nodeKeyFrom, nodeKeyTo));

            return readResult.Reader.AsStream();
        }

        public IEnumerable<string> GetAdjacent(string nodeKey)
        {
            using (var iterator = _snapshot.Iterate(_edgeTreeName))
            {
                iterator.RequiredPrefix = nodeKey;
                if (!iterator.Seek(Slice.BeforeAllKeys))
                    yield break;

                do
                {
                    var key = Util.ParseEdgeTreeKey(iterator.CurrentKey.ToString());
                    yield return key.NodeKeyTo;
                } while (iterator.MoveNext());
            }
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
            if (_objectsToDispose != null)
            {
                foreach (var disposable in _objectsToDispose.Values)
                    if (disposable != null)
                        disposable.Dispose();
                _objectsToDispose = null;
            }

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
            if(_snapshot != null || _objectsToDispose != null)
                Trace.WriteLine("Disposal for Session object was not called, disposing from finalizer. Stack Trace: " + new StackTrace());

            Dispose(false);
        }

    }
}