using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms
{
    public abstract class BaseRootedAlgorithm : BaseAlgorithm
    {
        public BaseRootedAlgorithm(CancellationToken cancelToken)
            : base(cancelToken)
        {
        }

        protected abstract Node GetRootNode(Transaction tx);
    }
}
