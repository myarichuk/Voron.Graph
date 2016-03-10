using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
    public unsafe class Traversal
    {
		public enum Strategy
		{
			Bfs,
			Dfs
		}


		private readonly Lazy<Transaction> _tx;
		private readonly Strategy _traversalStrategy;

		private readonly int _minDepth;
		private readonly int _maxDepth;

		private readonly Func<int, bool> _traversalDepthPredicate;

		private readonly Func<TableValueReader, bool> _edgePredicate;
		private readonly Func<TableValueReader, bool> _traversalContinuationPredicate;

		private bool _traversed;
		private long _depth;

		private readonly HashSet<long> _visitedVertices = new HashSet<long>();

	    public Traversal(Lazy<Transaction> tx, 
						 Strategy traversalStrategy, 
						 int minDepth, 
						 int maxDepth, 
						 Func<int, bool> traversalDepthPredicate, 
						 Func<TableValueReader, bool> edgePredicate, 
						 Func<TableValueReader, bool> traversalContinuationPredicate)
	    {

		    _tx = tx;
		    _traversalStrategy = traversalStrategy;
		    _minDepth = minDepth;
		    _maxDepth = maxDepth;
		    _traversalDepthPredicate = traversalDepthPredicate;
		    _edgePredicate = edgePredicate;
		    _traversalContinuationPredicate = traversalContinuationPredicate;
	    }

		public IEnumerable<long> Traverse(long startingVertexId,
			Action<TableValueReader> vertexVisitor = null,
			Action<TableValueReader> edgeVisitor = null)

		{
			if (_traversed)
				throw new AlreadyTraversedException("Traversal object has been used already, cannot traverse again.");
			var results = new List<long>();
			_traversed = true;
			using (var tx = _tx.Value)
			{
				Debug.Assert(tx.VoronTx.LowLevelTransaction.Flags !=
					TransactionFlags.ReadWrite);
				results.AddRange(InnerTraverse(tx,
					startingVertexId, 
					vertexVisitor, 
					edgeVisitor));
			}

			return results;
		}

		private IEnumerable<long> InnerTraverse(Transaction tx,
			long vertexId,
			Action<TableValueReader> vertexVisitor,
			Action<TableValueReader> edgeVisitor)
		{
			if (_depth > _maxDepth)
				return Enumerable.Empty<long>();

			_visitedVertices.Add(vertexId);

			if (vertexVisitor != null)
			{
				int size;
				var ptr = tx.VertexTable.DirectRead(vertexId, out size);
				vertexVisitor(new TableValueReader(ptr, size));
			}

			var result = tx.EdgeTable.SeekForwardFrom(
				Constants.Indexes.EdgeTable.FromToIndex,
					new Slice((byte*)&vertexId, sizeof(long)), true);

			var adjacentVertices = result.SelectMany(x =>
				x.Results.Select(r =>
				{

					if (edgeVisitor != null)
						edgeVisitor(r);

					int _;
					return *(long*)r.Read((int)EdgeTableFields.ToKey, out _);
				}));

			var intermediateResults = Enumerable.Empty<long>();
			foreach (var adjacentVertex in adjacentVertices)
			{
				if (!_visitedVertices.Contains(adjacentVertex))
				{
					_depth++;
					var currentInnerResults = InnerTraverse(tx,
						adjacentVertex,
						vertexVisitor,
						edgeVisitor);
					_depth--;

					intermediateResults = intermediateResults.Concat(currentInnerResults);
				}
			}

			if (_depth < _minDepth)
				return intermediateResults;

			return new[] { vertexId }.Concat(intermediateResults);
		}

	}
}
