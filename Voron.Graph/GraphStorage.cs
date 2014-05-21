using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Impl;
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

        public GraphStorage(string graphName, StorageEnvironment storageEnvironment)
        {
            if (String.IsNullOrWhiteSpace(graphName)) throw new ArgumentNullException("graphName");
            if (storageEnvironment == null) throw new ArgumentNullException("storageEnvironment");
            _nodeTreeName = graphName + Constants.NodeTreeNameSuffix;
            _edgeTreeName = graphName + Constants.EdgeTreeNameSuffix;
            _disconnectedNodesTreeName = graphName + Constants.DisconnectedNodesTreeName;
            _storageEnvironment = storageEnvironment;
            
            CreateConventions();
            CreateSchema();
            CreateCommandAndQueryInstances();
            _nextId = GetLatestStoredNodeKey();
        }

        public Transaction NewTransaction(TransactionFlags flags, TimeSpan? timeout = null)
        {
            var voronTransaction = _storageEnvironment.NewTransaction(flags, timeout);
            return new Transaction(voronTransaction, _nodeTreeName, _edgeTreeName, _disconnectedNodesTreeName);
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

        public GraphAdminQueries AdminQueries { get; private set; }

        private void CreateSchema()
        {
            using (var tx = _storageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                _storageEnvironment.CreateTree(tx, _nodeTreeName);
                _storageEnvironment.CreateTree(tx, _edgeTreeName);
                _storageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                tx.Commit();
            }
        }

        public void CreateCommandAndQueryInstances()
        {
            AdminQueries = new GraphAdminQueries();
            Queries = new GraphQueries();
            Commands = new GraphCommands(Queries, Conventions);
        }

        public void Dispose()
        {
        }
    }
}
