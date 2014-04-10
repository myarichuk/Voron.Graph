using System;
using System.IO;
using Voron.Impl;

namespace Voron.Graph
{
    public class Session : ISession
    {
        private WriteBatch _writeBatch;
        private readonly SnapshotReader _snapshot;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _nodesWithEdgesTreeName;
        private readonly Action<WriteBatch> _writerFunc;
        private readonly string _disconnectedNodesTreeName;

        internal Session(SnapshotReader snapshot, string nodeTreeName, string edgeTreeName, string nodesWithEdgesTreeName, string disconnectedNodesTreeName, Action<WriteBatch> writerFunc)
        {
            _snapshot = snapshot;
            _nodeTreeName = nodeTreeName;
            _edgeTreeName = edgeTreeName;
            _nodesWithEdgesTreeName = nodesWithEdgesTreeName;
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

        internal string NodesWithEdgesTreeName
        {
            get { return _nodesWithEdgesTreeName; }
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

        public void PutEdge(string nodeKeyFrom, string nodeKeyTo)
        {
            if (String.IsNullOrWhiteSpace(nodeKeyFrom)) throw new ArgumentNullException("nodeKeyFrom");
            if (String.IsNullOrWhiteSpace(nodeKeyTo)) throw new ArgumentNullException("nodeKeyTo");

            _writeBatch.MultiAdd(nodeKeyFrom, nodeKeyTo, _edgeTreeName);
            _writeBatch.Add(nodeKeyFrom, Stream.Null, _nodesWithEdgesTreeName);
            _writeBatch.Add(nodeKeyTo, Stream.Null, _nodesWithEdgesTreeName);

            _writeBatch.Delete(nodeKeyFrom, _disconnectedNodesTreeName);
            _writeBatch.Delete(nodeKeyTo, _disconnectedNodesTreeName);
        }

        public void DeleteNode(string nodeKey)
        {
            if (String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            _writeBatch.Delete(nodeKey, _nodeTreeName);

            ushort? version;
            if(_snapshot.Contains(_nodesWithEdgesTreeName,nodeKey,out version,_writeBatch))
                _writeBatch.Delete(nodeKey, _nodesWithEdgesTreeName);

            if(_snapshot.Contains(_disconnectedNodesTreeName,nodeKey,out version,_writeBatch))
                _writeBatch.Delete(nodeKey, _disconnectedNodesTreeName);
        }

        public void DeleteEdge(string nodeKeyFrom, string nodeKeyTo)
        {
            if (String.IsNullOrWhiteSpace(nodeKeyFrom)) throw new ArgumentNullException("nodeKeyFrom");
            if (String.IsNullOrWhiteSpace(nodeKeyTo)) throw new ArgumentNullException("nodeKeyTo");

            _writeBatch.MultiDelete(nodeKeyFrom, nodeKeyTo, _edgeTreeName);
            if (IsMultiTreeEmpty(nodeKeyFrom, _edgeTreeName))
            {
                _writeBatch.Delete(nodeKeyFrom, _nodesWithEdgesTreeName);
                _writeBatch.Add(nodeKeyFrom,Stream.Null,_disconnectedNodesTreeName);
            }

            ushort? version;
            if(_snapshot.Contains(_nodesWithEdgesTreeName,nodeKeyFrom,out version,_writeBatch))
                _writeBatch.Delete(nodeKeyFrom, _nodesWithEdgesTreeName);
        }

        public Stream Get(string nodeKey)
        {
            if(String.IsNullOrWhiteSpace(nodeKey)) throw new ArgumentNullException("nodeKey");

            var readResult = _snapshot.Read(_nodeTreeName, nodeKey, _writeBatch);
            return readResult.Reader.AsStream();
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

        private bool IsMultiTreeEmpty(string multiTreeKey, string treeName)
        {
            using (var iterator = _snapshot.MultiRead(treeName,multiTreeKey))
                return iterator.Seek(Slice.BeforeAllKeys);
        }
    }
}