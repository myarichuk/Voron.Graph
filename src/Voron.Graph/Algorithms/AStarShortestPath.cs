using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph.Algorithms
{
	//note: this is not a thread-safe class
	public class AStarShortestPath : IShortestPath
	{
		private readonly Func<long, TableValueReader, double> _h;
		private readonly Func<long, long, TableValueReader, double> _g;
		private readonly GraphStorage _parent;

		private readonly HashSet<long> _openSet = new HashSet<long>();

		private long currentNode;
		internal AStarShortestPath(GraphStorage parent, 
			Func<long, long, TableValueReader, double> g,
			Func<long, TableValueReader, double> h)
		{
			_parent = parent;
			_g = g;
			_h = h;
		}

		public IEnumerable<long> FindPath(long startVertex, long endVertex)
		{
			_openSet.Clear();
			currentNode = startVertex;
			return _parent
				.Traverse()
				.StopWhen(_ => _openSet.Count == 0 || 
								currentNode == endVertex)
				.Execute(startVertex, 
					reader => //vertex visitor
					{				
					},
					reader => //edge visitor
					{
					});
		}
	}
}
