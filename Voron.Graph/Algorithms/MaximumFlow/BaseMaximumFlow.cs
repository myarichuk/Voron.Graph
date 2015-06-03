using System;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.MaximumFlow
{
	public abstract class BaseMaximumFlow : BaseAlgorithm
    {
        protected Func<Edge, long> _capacity;

        protected BaseMaximumFlow(Func<Edge, long> capacity)
        {
            _capacity = capacity;
        }

        public abstract long MaximumFlow();

        public abstract Task<long> MaximumFlowAsync();
    }
}
