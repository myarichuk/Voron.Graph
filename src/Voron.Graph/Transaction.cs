using System;
using Voron.Data.BTrees;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public class Transaction : IDisposable
	{
		private readonly Impl.Transaction _tx;
		private bool _isDisposed;
		private StorageEnvironment _env;

		private Table _adjacencyListTable;
		private Tree _vertexTree;
		private Tree _etagToVertexTree;
		private Tree _etagToAdjacencyTree;

		internal Transaction(StorageEnvironment env, Impl.Transaction tx)
		{
			_tx = tx;
			_env = env;
		}

		public Table AdjacencyListTable => 
			(_adjacencyListTable != null) ? 
				_adjacencyListTable : 
				(_adjacencyListTable = new Table(_env.AdjacencyListSchema, 
					Constants.Schema.AdjacencyList, _tx));

		public Tree VertexTree => (_vertexTree != null) ?
			_vertexTree : 
			(_vertexTree = _tx.ReadTree(Constants.Schema.VertexTree));

		public Tree EtagToVertexTree => (_etagToVertexTree != null) ?
			_etagToVertexTree :
			(_etagToVertexTree = _tx.ReadTree(Constants.Schema.EtagToVertexTree));

		public Tree EtagToAdjacencyTree => (_etagToAdjacencyTree != null) ?
			_etagToAdjacencyTree :
			(_etagToAdjacencyTree = _tx.ReadTree(Constants.Schema.EtagToAdjacencyTree));

		internal Impl.Transaction VoronTx => _tx;

		public void Commit()
		{
			ThrowIfDisposed();
			_tx.Commit();
		}

		void ThrowIfDisposed()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(Transaction));
		}

		public void Dispose()
		{
			if (!_isDisposed)
			{
				_tx.Dispose();
				_isDisposed = true;
			}
			GC.SuppressFinalize(this);
		}

		~Transaction()
		{
			if (!_isDisposed)
				Dispose();
		}
	}
}
