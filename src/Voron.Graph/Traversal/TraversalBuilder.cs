using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
    public class TraversalBuilder
    {		

		private readonly Lazy<Transaction> _tx;
		private Traversal.Strategy _traversalStrategy = Traversal.Strategy.Bfs;

		private int _minDepth = -1;
		private int _maxDepth = int.MaxValue;
		private long _maxResults = 0; //default => no limit

		private Func<int, bool> _traversalDepthPredicate;

		private Func<TableValueReader, bool> _edgePredicate;
		private Func<TableValueReader, bool> _traversalContinuationPredicate;
		private Func<TableValueReader, bool> _traversalStopPredicate;

		public IEnumerable<long> Execute(long startingVertexId,
			Action<TableValueReader> vertexVisitor = null,
			Action<TableValueReader> edgeVisitor = null)
		{
			return new Traversal(_tx, _traversalStrategy,
					_minDepth, 
					_maxDepth,
					_maxResults,
					_traversalDepthPredicate, 
					_edgePredicate, 
					_traversalContinuationPredicate,					
					_traversalStopPredicate)
					.Traverse(startingVertexId,vertexVisitor,edgeVisitor);
		}

		internal TraversalBuilder(GraphStorage store)
		{
			_tx = new Lazy<Transaction>(store.ReadTransaction,false);			
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder WithMinDepth(int depth)
		{
			_minDepth = depth;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder WithMaxDepth(int depth)
		{
			_maxDepth = depth;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder WithMaxResults(long maxResultsCount)
		{
			_maxResults = maxResultsCount;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder WhereDepth(Func<int, bool> traversalDepthPredicate)
		{
			_traversalDepthPredicate = traversalDepthPredicate;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder WithStrategy(Traversal.Strategy traversalStrategy)
		{
			_traversalStrategy = traversalStrategy;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder TraverseEdgesThat(Func<TableValueReader, bool> edgePredicate)
		{
			_edgePredicate = edgePredicate;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder TraverseWhile(Func<TableValueReader, bool> traversalContinuationPredicate)
		{
			_traversalContinuationPredicate = traversalContinuationPredicate;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder StopWhen(Func<TableValueReader, bool> traversalStopPredicate)
		{
			_traversalStopPredicate = traversalStopPredicate;
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TraversalBuilder TraverseWhileDepth(Func<int, bool> traversalDepthPredicate)
		{
			_traversalDepthPredicate = traversalDepthPredicate;
			return this;
		}
	}
}
