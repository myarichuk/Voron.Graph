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
using Newtonsoft.Json.Linq;

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
        private readonly Conventions _conventions;
        private readonly object _syncObject = new object();

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc,Conventions conventions)
        {
            _snapshot = snapshot;
            _conventions = conventions;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _writerFunc = writerFunc;
            _writeBatch = new WriteBatch();
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
                        var node = new Node(key.ToInt64(), value.ToJObject());

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
                        var currentKey = key.ToEdgeTreeKey();
                        var jsonValue = value.Length > 0 ? value.ToJObject() : new JObject();

                        var edge = new Edge(currentKey.NodeKeyFrom, currentKey.NodeKeyTo, jsonValue, currentKey.Type);
                        
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

        public Node CreateNode(dynamic value)
        {
            var type = value.GetType();
            if (type.IsPrimitive || type.Name.Contains("String"))
                return CreateNode(JObject.FromObject(new { Value = value }));

            return CreateNode(JObject.FromObject(value));
        }
        

        public Node CreateNode(JObject value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var key = _conventions.IdGenerator.NextId();

            var nodeKey = key.ToSlice();

            _writeBatch.Add(nodeKey, value.ToStream(), _nodeTreeName);
            _writeBatch.Add(nodeKey, Stream.Null, _disconnectedNodesTreeName);

            return new Node(key, value);
        }

        public Edge CreateEdgeBetween(Node nodeFrom, Node nodeTo, dynamic value, ushort type = 0)
        {
            var valueType = value.GetType();
            if (valueType.IsPrimitive || valueType.Name.Contains("String"))
                return CreateEdgeBetween(nodeFrom, nodeTo,JObject.FromObject(new { Value = value }),type);

            return CreateEdgeBetween(nodeFrom, nodeTo, JObject.FromObject(value), type);
        }

        public Edge CreateEdgeBetween(Node nodeFrom, Node nodeTo, JObject value = null, ushort type = 0)
        {
            if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (nodeTo == null) throw new ArgumentNullException("nodeTo");

            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value);
            _writeBatch.Add(edge.Key.ToSlice(), value.ToStream() ?? Stream.Null, _edgeTreeName);

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
                        var adjacentNode = LoadNode(edgeKey.NodeKeyTo);
                        yield return adjacentNode;
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


        public Node LoadNode(long nodeKey)
        {
            var readResult = _snapshot.Read(_nodeTreeName, nodeKey.ToSlice(),_writeBatch);
            using (var valueStream = readResult.Reader.AsStream())
                return new Node(nodeKey, valueStream.ToJObject());
        }

        
        public IEnumerable<Edge> GetEdgesBetween(Node nodeFrom, Node nodeTo,Func<ushort,bool> typePredicate = null)
        {
            using (var edgeIterator = _snapshot.Iterate(_edgeTreeName, _writeBatch))
            {
                edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);
                if (!edgeIterator.Seek(edgeIterator.RequiredPrefix))
                    yield break;

                do
                {
                    var edgeTreeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (typePredicate != null && !typePredicate(edgeTreeKey.Type))
                        continue;

                    var valueReader = edgeIterator.CreateReaderForCurrent();
                    using (var valueStream = valueReader.AsStream() ?? Stream.Null)
                    {
                        var jsonValue = valueStream.Length > 0 ? valueStream.ToJObject() : new JObject();
                        yield return new Edge(edgeTreeKey, valueStream.ToJObject());
                    }

                } while (edgeIterator.MoveNext());
            }
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

            if (isDisposing)
                GC.SuppressFinalize(this);
        }

        ~Session()
        {
#if DEBUG
            if (_snapshot != null)
                Trace.WriteLine("Disposal for Session object was not called, disposing from finalizer. Stack Trace: " + new StackTrace());
#endif
            Dispose(false);
        }       
    }
}