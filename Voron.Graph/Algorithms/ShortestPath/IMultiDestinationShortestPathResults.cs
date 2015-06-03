using System.Collections.Generic;

namespace Voron.Graph.Algorithms.ShortestPath
{
	public interface IMultiDestinationShortestPathResults
    {
        IEnumerable<long> GetShortestPathToNode(Node node);
    }
}
