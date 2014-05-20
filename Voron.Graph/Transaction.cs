using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public class Transaction : IDisposable
    {
        public Voron.TransactionFlags Flags
        {
            get
            {
                return VoronTransaction.Flags;
            }
        }

        internal Voron.Impl.Transaction VoronTransaction { get; private set; }

        internal Transaction(Voron.Impl.Transaction voronTransaction)
        {
            if (voronTransaction == null)
                throw new ArgumentNullException("voronTransaction");
            VoronTransaction = voronTransaction;
        }

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
