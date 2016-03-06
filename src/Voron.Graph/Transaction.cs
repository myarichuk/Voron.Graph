﻿using System;
using Voron.Data.BTrees;
using Voron.Data.RawData;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public class Transaction : IDisposable
	{
		private readonly Impl.Transaction _tx;
		private bool _isDisposed;
		private GraphStorage _storage;

		private Table _adjacencyListTable;
		private Table _vertexTable;
		private Tree _etagToVertexTree;
		private Tree _etagToAdjacencyTree;

		private ActiveRawDataSmallSection _systemDataSection;

		internal Transaction(GraphStorage env, Impl.Transaction tx)
		{
			_tx = tx;
			_storage = env;
		}

		public ActiveRawDataSmallSection SystemDataSection =>
			(_systemDataSection != null) ?
				_systemDataSection :
			(_systemDataSection = new ActiveRawDataSmallSection(_tx.LowLevelTransaction, _storage.SystemDataSectionPage));

		public Table AdjacencyListTable => 
			(_adjacencyListTable != null) ? 
				_adjacencyListTable : 
				(_adjacencyListTable = new Table(_storage.AdjacencyListSchema, 
					Constants.Schema.AdjacencyList, _tx));

		public Table VertexTable => (_vertexTable != null) ?
			_vertexTable : 
			(_vertexTable = new Table(_storage.VerticesSchema, 
				Constants.Schema.Vertices, _tx));

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