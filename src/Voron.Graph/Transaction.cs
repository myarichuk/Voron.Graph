using System;
using System.Text;
using Voron.Data.BTrees;
using Voron.Data.RawData;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public class Transaction : IDisposable
	{
		private readonly Impl.Transaction _tx;
		private bool _isDisposed;
		internal GraphStorage _storage;

		private Table _edgesTable;
		private Table _vertexTable;
		private Tree _etagToVertexTree;
		private Tree _etagToAdjacencyTree;
		private Tree _metadataTree;
		private long _nextId;

		internal long NextId
		{
			get
			{
				return _nextId;
			}

			set
			{
				_nextId = value;
				_hasIdChanged = true;
			}
		}

		private bool _hasIdChanged;
		private readonly Slice _nextIdKey;

		internal Transaction(GraphStorage env, Impl.Transaction tx)
		{
			_tx = tx;
			_storage = env;

			_nextIdKey = Slice.From(env.ByteStringContext, Constants.NextIdKey);
			var readResult = MetadataTree.Read(_nextIdKey);
			NextId = (readResult == null) ? 0 : readResult.Reader.ReadBigEndianInt64();
		}

		internal GraphStorage Storage => _storage;
	
		internal Table EdgeTable => 
			(_edgesTable != null) ? 
				_edgesTable : 
				(_edgesTable = new Table(_storage.EdgesSchema, 
					Constants.Schema.Edges, _tx));

		internal Table VertexTable => (_vertexTable != null) ?
			_vertexTable : 
			(_vertexTable = new Table(_storage.VerticesSchema, 
				Constants.Schema.Vertices, _tx));


		internal Tree MetadataTree => (_metadataTree != null) ?
			_metadataTree :
			(_metadataTree = _tx.CreateTree(Constants.MetadataTree));

		internal Tree EtagToVertexTree => (_etagToVertexTree != null) ?
			_etagToVertexTree :
			(_etagToVertexTree = _tx.ReadTree(Constants.Schema.EtagToVertexTree));

		internal Tree EtagToAdjacencyTree => (_etagToAdjacencyTree != null) ?
			_etagToAdjacencyTree :
			(_etagToAdjacencyTree = _tx.ReadTree(Constants.Schema.EtagToAdjacencyTree));

		internal Impl.Transaction VoronTx => _tx;

		

		public void Commit()
		{
			ThrowIfDisposed();

			if (_hasIdChanged)
			{
				var valueWriter = new SliceWriter(sizeof(long));
				valueWriter.WriteBigEndian(_nextId);
				MetadataTree.Add(_nextIdKey, valueWriter.CreateSlice(_storage.ByteStringContext));
			}

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
