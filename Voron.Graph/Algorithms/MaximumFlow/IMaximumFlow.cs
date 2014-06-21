using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.MaximumFlow
{
    public interface IMaximumFlow
    {
        long MaximumFlow(long startNodeKey, long endNodeKey);

        Task<long> MaximumFlowAsync(long startNodeKey, long endNodeKey);
    }
}
