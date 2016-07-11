using Sparrow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public class Traversal
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
		private readonly long _maxResults;

		private readonly Func<int, bool> _traversalDepthPredicate;

		private readonly Func<TableValueReader, bool> _edgePredicate;
		private readonly Func<TableValueReader, bool> _traversalContinuationPredicate;

		private bool _traversed;
		private int _depth;
		private long _traversedResults;

		private readonly HashSet<long> _visitedVertices = new HashSet<long>();
		private Func<TableValueReader, bool> _traversalStopPredicate;

		public Traversal(Lazy<Transaction> tx, 
						 Strategy traversalStrategy, 
						 int minDepth, 
						 int maxDepth,
						 long maxResults,
						 Func<int, bool> traversalDepthPredicate, 
						 Func<TableValueReader, bool> edgePredicate, 
						 Func<TableValueReader, bool> traversalContinuationPredicate,
						 Func<TableValueReader, bool> traversalStopPredicate)
		{

			_tx = tx;
			_traversalStrategy = traversalStrategy;
			_minDepth = minDepth;
			_maxDepth = maxDepth;
			_maxResults = maxResults;
			_traversalDepthPredicate = traversalDepthPredicate;
			_edgePredicate = edgePredicate;
			_traversalContinuationPredicate = traversalContinuationPredicate;
			_traversalStopPredicate = traversalStopPredicate;
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
				if (_traversalStrategy == Strategy.Bfs)
				{
					results.AddRange(InnerTraverseBFS(tx,
						startingVertexId,
						vertexVisitor,
						edgeVisitor));
				}
				else
				{
					results.AddRange(InnerTraverseDFS(tx,
						startingVertexId,
						vertexVisitor,
						edgeVisitor));
				}
			}

			return results;
		}

		private IEnumerable<long> InnerTraverseDFS(Transaction tx,
					long vertexId,
					Action<TableValueReader> vertexVisitor,
					Action<TableValueReader> edgeVisitor)
		{
			if (_depth > _maxDepth ||
				(_traversalDepthPredicate != null &&
				 !_traversalDepthPredicate(_depth)))
				return Enumerable.Empty<long>();

			var adjacentVertices = GetAdjacent(tx, vertexId, edgeVisitor);
			var intermediateResults = Enumerable.Empty<long>();
			_visitedVertices.Add(vertexId);
			foreach (var adjacentVertex in adjacentVertices.Where(vId => !_visitedVertices.Contains(vId)))
			{
				_depth++;
				_visitedVertices.Add(adjacentVertex);
				var currentInnerResults = InnerTraverseDFS(tx,
					adjacentVertex,
					vertexVisitor,
					edgeVisitor);
				_depth--;

				intermediateResults = intermediateResults.Concat(currentInnerResults);
				
			}

			if (vertexVisitor != null || _traversalStopPredicate != null)
			{
				var vertexTableReader = GetReaderForVertex(tx, vertexId);
#pragma warning disable CC0016 // Copy Event To Variable Before Fire
				//this warning is likely a Roslyn bug and should be here,
				//but it is.. hence the warning disable
				if (_traversalStopPredicate?.Invoke(vertexTableReader) ?? default(bool))
					return Enumerable.Empty<long>();
#pragma warning restore CC0016 // Copy Event To Variable Before Fire

				vertexVisitor?.Invoke(vertexTableReader);
			}

			_traversedResults++;
			if (_maxResults > 0 && _traversedResults >= _maxResults)
				return new[] { vertexId };


			if (_depth < _minDepth)
				return intermediateResults;

			return new[] { vertexId }.Concat(intermediateResults);
		}

		private IEnumerable<long> InnerTraverseBFS(Transaction tx,
			long vertexId,
			Action<TableValueReader> vertexVisitor,
			Action<TableValueReader> edgeVisitor)
		{
			if (_depth > _maxDepth ||
				(_traversalDepthPredicate != null &&
				 !_traversalDepthPredicate(_depth)))
				return Enumerable.Empty<long>();


			_visitedVertices.Add(vertexId);

			//TODO : finish all the limitation implementations
			// and write relevant tests

			if (vertexVisitor != null || _traversalStopPredicate != null)
			{
				var vertexTableReader = GetReaderForVertex(tx, vertexId);
#pragma warning disable CC0016 // Copy Event To Variable Before Fire
				//this warning is likely a Roslyn bug and should be here,
				//but it is.. hence the warning disable
				if (_traversalStopPredicate?.Invoke(vertexTableReader) ?? default(bool))
					return Enumerable.Empty<long>();
#pragma warning restore CC0016 // Copy Event To Variable Before Fire

				vertexVisitor?.Invoke(vertexTableReader);
			}

			_traversedResults++;
			if (_maxResults > 0 && _traversedResults >= _maxResults)
				return new[] { vertexId };

			var adjacentVertices = GetAdjacent(tx, vertexId, edgeVisitor);
			var intermediateResults = Enumerable.Empty<long>();
			foreach (var adjacentVertex in adjacentVertices)
			{
				if (!_visitedVertices.Contains(adjacentVertex))
				{
					_depth++;
					var currentInnerResults = InnerTraverseBFS(tx,
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

#pragma warning disable CC0091 // Use static method
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe TableValueReader GetReaderForVertex(Transaction tx, long id) =>
			tx.VertexTable.ReadByKey(id.ToSlice(tx._storage.ByteStringContext));
#pragma warning restore CC0091 // Use static method


		private unsafe IEnumerable<long> GetAdjacent(Transaction tx, long id, Action<TableValueReader> edgeVisitor)
		{
			ByteString seekKey;
			try
			{
				seekKey = tx.Storage.ByteStringContext.FromPtr((byte*)&id, sizeof(long));
				var seekResult = tx.EdgeTable.SeekForwardFrom(tx.Storage.FromToIndex,
									new Slice(seekKey), true);

				var adjacentVertices = seekResult.SelectMany(x =>
					x.Results
					 .Where(r => _edgePredicate == null ||
						(_edgePredicate != null && _edgePredicate(r)))
					 .Select(r =>
					 {
						 edgeVisitor?.Invoke(r);

						 int _;
						 return *(long*)r.Read((int)EdgeTableFields.ToKey, out _);
					 })).ToList();

				return adjacentVertices;
			}
			finally
			{
				tx.Storage.ByteStringContext.Release(ref seekKey);
			}
		}
	}
}
