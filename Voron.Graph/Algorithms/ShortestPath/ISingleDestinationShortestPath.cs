using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public interface ISingleDestinationShortestPath
    {
        IEnumerable<long> Execute();
        Task<IEnumerable<long>> ExecuteAsync();
    }
}
