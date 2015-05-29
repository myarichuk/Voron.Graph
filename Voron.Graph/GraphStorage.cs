using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using Voron.Graph.Extensions;

namespace Voron.Graph
{
    public partial class GraphStorage : IDisposable
    {
        private readonly string _graphName;
        private readonly StorageEnvironment _storageEnvironment;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _disconnectedNodesTreeName;
        private readonly string _keyByEtagTreeName;
        private readonly string _graphMetadataKey;
        private long _nextId;

        public GraphStorage(string graphName, StorageEnvironment storageEnvironment)
        {
            if (String.IsNullOrWhiteSpace(graphName)) throw new ArgumentNullException("graphName");
            if (storageEnvironment == null) throw new ArgumentNullException("storageEnvironment");
            _nodeTreeName = graphName + Constants.NodeTreeNameSuffix;
            _edgeTreeName = graphName + Constants.EdgeTreeNameSuffix;
            _disconnectedNodesTreeName = graphName + Constants.DisconnectedNodesTreeNameSuffix;
            _keyByEtagTreeName = graphName + Constants.KeyByEtagTreeNameSuffix;

            _graphName = graphName;
            _storageEnvironment = storageEnvironment;
            _graphMetadataKey = graphName + Constants.GraphMetadataKeySuffix;

			CreateConventions();
            CreateSchema();

			CreateCommandAndQueryInstances();

            _nextId = GetLatestStoredNodeKey();
        }

        public Transaction NewTransaction(TransactionFlags flags, TimeSpan? timeout = null)
        {
            var voronTransaction = StorageEnvironment.NewTransaction(flags, timeout);
            return new Transaction(voronTransaction, 
                _nodeTreeName, 
                _edgeTreeName, 
                _disconnectedNodesTreeName, 
                _keyByEtagTreeName, 
                _graphMetadataKey,
                _nextId);
        }

        private long GetLatestStoredNodeKey()
        {
            using(var tx = StorageEnvironment.NewTransaction(TransactionFlags.Read))
            {
                var tree = tx.ReadTree(_nodeTreeName);
                using(var iterator = tree.Iterate())
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

        public string GraphName
        {
            get { return _graphName; }
        }

        internal StorageEnvironment StorageEnvironment
        {
            get { return _storageEnvironment; }
        }

		public GraphAdmin Admin { get; private set; }

		private void CreateSchema()
        {
            using (var tx = StorageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                StorageEnvironment.CreateTree(tx, _nodeTreeName);
                StorageEnvironment.CreateTree(tx, _edgeTreeName);
                StorageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                StorageEnvironment.CreateTree(tx, _keyByEtagTreeName);

                if (tx.State.Root.ReadVersion(_graphMetadataKey) == 0)
                    tx.State.Root.Add(_graphMetadataKey, (new JObject()).ToStream());

                tx.Commit();
            }
        }


        public void CreateCommandAndQueryInstances()
        {
			var runInMemory = _storageEnvironment.Options is StorageEnvironmentOptions.PureMemoryStorageEnvironmentOptions;
            Admin = new GraphAdmin();
		}

		public void Dispose()
        {
        }
    }
}
