using System;
using Voron.Trees;

namespace Voron.Graph
{
    /// <summary>
    /// Encapsulates Voron transaction and provides clean access to Voron.Graph trees. 
    /// There can be only one ReadWrite tx in any given time (enforced with semaphore)
    /// </summary>
    /// <remarks>
    /// might be counter-intuitive, but the transaction does not offer change tracking
    /// </remarks>
    public class Transaction : IDisposable
    {
        private bool _isDisposed;

        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public TransactionFlags Flags
        {
            get
            {
                return VoronTransaction.Flags;
            }
        }

        internal Voron.Impl.Transaction VoronTransaction { get; private set; }

        public Transaction(Voron.Impl.Transaction voronTransaction, 
            string nodeTreeName, 
            string edgesTreeName, 
            string disconnectedNodesTreeName,
            string keyByEtagTreeName,
            string nodeMetadataTreeName,
            string graphMetadataKey)
        {
            _isDisposed = false;
            if (voronTransaction == null)
                throw new ArgumentNullException("voronTransaction");
            VoronTransaction = voronTransaction;

            NodeTree = voronTransaction.ReadTree(nodeTreeName);
            EdgeTree = voronTransaction.ReadTree(edgesTreeName);
            DisconnectedNodeTree = voronTransaction.ReadTree(disconnectedNodesTreeName);
            KeyByEtagTree = voronTransaction.ReadTree(keyByEtagTreeName);
            NodeMetadataTree = voronTransaction.ReadTree(nodeMetadataTreeName);

            SystemTree = voronTransaction.State.Root;
            GraphMetadataKey = graphMetadataKey;
        }

        internal string GraphMetadataKey { get; private set; }

        internal Tree SystemTree { get; private set; }

        internal Tree NodeMetadataTree { get; private set; }

        internal Tree NodeTree { get; private set; }

        internal Tree EdgeTree { get; private set; }

        internal Tree DisconnectedNodeTree { get; private set; }

        internal Tree KeyByEtagTree { get; private set; }

        public void Dispose()
        {
            _isDisposed = true;
            VoronTransaction.Dispose();
        }

        public void Rollback()
        {
            VoronTransaction.Rollback();
        }

        public void Commit()
        {
            VoronTransaction.Commit();
        }
    }
}
