using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public interface ISingleSourceShortestPathResults
    {
        IEnumerable<long> GetShortestPathToNode(Node node);
    }
}
