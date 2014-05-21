using System;
using Voron.Trees;

namespace Voron.Graph
{
    public class Transaction : IDisposable
    {
        public TransactionFlags Flags
        {
            get
            {
                return VoronTransaction.Flags;
            }
        }

        internal Voron.Impl.Transaction VoronTransaction { get; private set; }

        public Transaction(Voron.Impl.Transaction voronTransaction, string nodeTreeName, string edgesTreeName, string disconnectedNodesTreeName)
        {
            if (voronTransaction == null)
                throw new ArgumentNullException("voronTransaction");
            VoronTransaction = voronTransaction;

            NodeTree = voronTransaction.ReadTree(nodeTreeName);
            EdgeTree = voronTransaction.ReadTree(edgesTreeName);
            DisconnectedNodeTree = voronTransaction.ReadTree(disconnectedNodesTreeName);
        }

        public Tree NodeTree { get; private set; }

        public Tree EdgeTree { get; private set; }

        public Tree DisconnectedNodeTree { get; private set; }

        public void Dispose()
        {
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
