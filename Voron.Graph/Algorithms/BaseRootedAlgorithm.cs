using System.Threading;

namespace Voron.Graph.Algorithms
{
    public abstract class BaseRootedAlgorithm : BaseAlgorithm
    {
	    protected BaseRootedAlgorithm(CancellationToken cancelToken)
            : base(cancelToken)
        {
        }

        protected abstract Node GetDefaultRootNode(Transaction tx);
    }
}
