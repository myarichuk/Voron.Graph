using System.Collections.Generic;

namespace Voron.Graph.Algorithms
{
	public interface IShortestPath
	{
		IEnumerable<long> FindPath(long startVertex, long endVertex); 
	}
}
