using Sparrow;
using Sparrow.Logging;
using System;
using System.Runtime.CompilerServices;
using Voron.Data.BTrees;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public unsafe partial class GraphStorage : IDisposable
	{		
		private readonly bool _ownsStorageEnvironment;
		private bool _isDisposed;
		private readonly StorageEnvironment _env;

		private readonly ByteStringContext _byteStringContext;

		private TableSchema _edgesSchema;
		private TableSchema _verticesSchema;

		internal TableSchema EdgesSchema => _edgesSchema;
		internal TableSchema VerticesSchema => _verticesSchema;

		private GraphAdvanced _advanced;
		public GraphAdvanced Advanced => (_advanced != null) ? _advanced : (_advanced = new GraphAdvanced(this));

		private GraphAdmin _admin;

		public TableSchema.SchemaIndexDef FromToIndex { get; private set; }

		internal ByteStringContext ByteStringContext => _byteStringContext;

		public GraphAdmin Admin => (_admin != null) ? _admin : (_admin = new GraphAdmin());

		public GraphStorage()
			: this(new StorageEnvironment(StorageEnvironmentOptions.CreateMemoryOnly(), new LoggerSetup(String.Empty,LogMode.None)), true)
		{
		}

		//TODO: consider adding logging support
		public GraphStorage(string path, string tempPath = null, string journalPath = null)
			: this(new StorageEnvironment(StorageEnvironmentOptions.ForPath(path, tempPath, journalPath),new LoggerSetup(String.Empty,LogMode.None)), true)
		{
		}

		public GraphStorage(StorageEnvironment env,bool ownsStorageEnvironment)
		{
			_env = env;
			this._ownsStorageEnvironment = ownsStorageEnvironment;
			_byteStringContext = new ByteStringContext();

			var fromToIndexByteString = _byteStringContext.From(nameof(FromToIndex));
			FromToIndex = new TableSchema.SchemaIndexDef
			{
				Name = nameof(FromToIndex),
				NameAsSlice = new Slice(fromToIndexByteString),
				StartIndex = (int)EdgeTableFields.FromKey,
				Count = 2,
				IsGlobal = true
			};

			CreateSchema();
		}

		public Transaction ReadTransaction()
		{
			return new Transaction(this,_env.ReadTransaction(_byteStringContext));
		}

		public Transaction WriteTransaction()
		{
			return new Transaction(this,_env.WriteTransaction(_byteStringContext));
		}

		private void CreateSchema()
		{
			using (var tx = _env.WriteTransaction())
			{
				var edgeEtagNameByteString = _byteStringContext.From("EdgeKey");
				_edgesSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = edgeEtagNameByteString.ToString(),
						NameAsSlice = new Slice(edgeEtagNameByteString),
						StartIndex = (int)EdgeTableFields.Key,
						IsGlobal = true
					})
					.DefineIndex(FromToIndex.Name,FromToIndex);

				_edgesSchema.Create(tx, Constants.Schema.Edges);

				//for long-term system related storage
				var systemTree = tx.CreateTree(Constants.Schema.SystemDataTree);

				var VertexIdByteString = _byteStringContext.From("VertexKey");
				_verticesSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = VertexIdByteString.ToString(),
						NameAsSlice = new Slice(VertexIdByteString),
						StartIndex = (int)VertexTableFields.Key,
						IsGlobal = true
					});

				_verticesSchema.Create(tx, Constants.Schema.Vertices);

				tx.CreateTree(Constants.Schema.EtagToAdjacencyTree);
				tx.CreateTree(Constants.Schema.EtagToVertexTree);

				tx.CreateTree(Constants.MetadataTree); 
				
				tx.Commit();
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ThrowIfDisposed()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(GraphStorage));
		}

		public void Dispose()
		{
			if (!_isDisposed)
			{
				if(_ownsStorageEnvironment)
					_env.Dispose();
				_isDisposed = true;
				_byteStringContext.Dispose();
				_edgesSchema.Dispose();
				_verticesSchema.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		~GraphStorage()
		{
			if (!_isDisposed)
				Dispose();
		}		
	}
}
