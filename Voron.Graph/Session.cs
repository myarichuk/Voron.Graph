using System;
using System.Collections.Generic;
using System.IO;
using Voron.Impl;
using System.Linq;
using System.Text;

namespace Voron.Graph
{
    public class Session : ISession
    {
        private WriteBatch _writeBatch;
        private readonly SnapshotReader _snapshot;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly Action<WriteBatch> _writerFunc;
        private readonly string _disconnectedNodesTreeName;

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc)
        {
            _snapshot = snapshot;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _writerFunc = writerFunc;
            _writeBatch = new WriteBatch();
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

        public Stream Get(string nodeKey)
        {
            if(String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            var readResult = _snapshot.Read(_nodeTreeName, nodeKey, _writeBatch);
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
            if (_snapshot != null)
                _snapshot.Dispose();
        }     



    }
}