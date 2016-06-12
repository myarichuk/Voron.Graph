using System.Collections.Generic;

namespace Voron.Graph.Algorithms
{
	public interface IShortestPath
	{
		IEnumerable<long> Execute(long startVertex, long endVertex); 
	}
}
