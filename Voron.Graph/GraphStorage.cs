using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using Voron.Graph.Extensions;
using Voron.Graph.Impl;

namespace Voron.Graph
{
    public class GraphStorage : IDisposable
    {
        private readonly string _graphName;
        private readonly StorageEnvironment _storageEnvironment;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _disconnectedNodesTreeName;
        private readonly string _keyByEtagTreeName;
        private readonly string _graphMetadataKey;
        private readonly HashSet<string> _indexedProperties;
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

            _indexedProperties = new HashSet<string>();
            CreateConventions();
            CreateSchema();
            CreateCommandAndQueryInstances();

            using (var tx = NewTransaction(TransactionFlags.Read))
                _indexedProperties = Queries.GetFromSystemMetadata<HashSet<string>>(tx, Constants.IndexedPropertyListKey);
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

        public GraphCommands Commands { get; private set; }

        public GraphQueries Queries { get; private set; }

        public GraphAdminQueries AdminQueries { get; private set; }

        public IEnumerable<string> IndexedProperties
        {
            get { return _indexedProperties; }
        }

        public string GraphName
        {
            get { return _graphName; }
        }

        internal StorageEnvironment StorageEnvironment
        {
            get { return _storageEnvironment; }
        }

        private void CreateSchema()
        {
            using (var tx = StorageEnvironment.NewTransaction(TransactionFlags.ReadWrite))
            {
                StorageEnvironment.CreateTree(tx, _nodeTreeName);
                StorageEnvironment.CreateTree(tx, _edgeTreeName);
                StorageEnvironment.CreateTree(tx, _disconnectedNodesTreeName);
                StorageEnvironment.CreateTree(tx, _keyByEtagTreeName);
                StorageEnvironment.CreateTree(tx, _metadataTreeName);

                if (tx.State.Root.ReadVersion(_graphMetadataKey) == 0)
                    tx.State.Root.Add(_graphMetadataKey, (new JObject()).ToStream());

                tx.Commit();
            }
        }

        public void AddIndexedProperties(params string[] propertyNames)
        {
            using (var tx = NewTransaction(TransactionFlags.ReadWrite))
            {
                _indexedProperties.UnionWith(propertyNames.Select(x => x.ToLower()));
                Commands.PutToSystemMetadata(tx,Constants.IndexedPropertyListKey,_indexedProperties);

                tx.Commit();
            }
        }

        public void RemoveIndexedProperties(params string[] propertyNames)
        {
            using (var tx = NewTransaction(TransactionFlags.ReadWrite))
            {
                _indexedProperties.ExceptWith(propertyNames.Select(x =>x.ToLower()));
                Commands.PutToSystemMetadata(tx, Constants.IndexedPropertyListKey, _indexedProperties);

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
