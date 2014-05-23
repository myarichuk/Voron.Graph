using System.Threading;

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
