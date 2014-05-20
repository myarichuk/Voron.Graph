using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Impl;

namespace Voron.Graph
{
    public class GraphStorage : IDisposable
    {
        private readonly StorageEnvironment _storageEnvironment;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _disconnectedNodesTreeName;
        private long _nextId;

        private readonly SemaphoreSlim _writeSequenceSync;

        public GraphStorage(string graphName, StorageEnvironment storageEnvironment)
        {
            if (String.IsNullOrWhiteSpace(graphName)) throw new ArgumentNullException("graphName");
            if (storageEnvironment == null) throw new ArgumentNullException("storageEnvironment");
            _nodeTreeName = graphName + Constants.NodeTreeNameSuffix;
            _edgeTreeName = graphName + Constants.EdgeTreeNameSuffix;
            _disconnectedNodesTreeName = graphName + Constants.DisconnectedNodesTreeName;
            _storageEnvironment = storageEnvironment;
            _writeSequenceSync = new SemaphoreSlim(1, 1);
            CreateConventions();
            CreateSchema();
            _nextId = GetLatestStoredNodeKey();
        }

        public Transaction NewTransaction(Voron.TransactionFlags flags, TimeSpan? timeout = null)
        {
            return new Transaction(_storageEnvironment.NewTransaction(flags, timeout));
        }

        private long GetLatestStoredNodeKey()
        {
            using(var tx = _storageEnvironment.NewTransaction(TransactionFlags.Read))
            {
                var tree = tx.ReadTree(_nodeTreeName);
                using(var iterator = tree.Iterate(tx))
                {
                    if (!iterator.Seek(Slice.AfterAllKeys))
                        return 0;

                    return iterator.CurrentKey.CreateReader().ReadBigEndianInt64();
                }
            }
        }

        private void CreateConventions()
        {
            Conventions = new Conventions
            {
                GetNextNodeKey = () => Interlocked.Increment(ref _nextId)
            };
        }

        public Conventions Conventions { get; private set; }

        public GraphCommands Commands { get; private set; }

        public GraphQueries Queries { get; private set; }

        private void CreateSchema()
        {
            using (var tx = _storageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                var nodeTree = _storageEnvironment.CreateTree(tx, _nodeTreeName);
                var edgeTree = _storageEnvironment.CreateTree(tx, _edgeTreeName);
                var disconnectedNodesTree = _storageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                tx.Commit();

                Queries = new GraphQueries(nodeTree, edgeTree, disconnectedNodesTree);
                Commands = new GraphCommands(Queries, nodeTree, edgeTree, disconnectedNodesTree, Conventions);
            }
        }

        public void Dispose()
        {
            _writeSequenceSync.Dispose();
        }
    }
}
