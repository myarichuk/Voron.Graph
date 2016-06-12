using Sparrow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Voron.Data.RawData;
using Voron.Data.Tables;
using Voron.Util.Conversion;

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
		private long _nextEdgeEtagEntry;
		private long _nextVertexEtagEntry;
		private long _nextIdEntry;
		private long _systemDataSectionPage;

		public TableSchema.SchemaIndexDef FromToIndex { get; private set; }

		internal long SystemDataSectionPage => _systemDataSectionPage;

		internal ByteStringContext ByteStringContext => _byteStringContext;

		public GraphAdmin Admin => (_admin != null) ? _admin : (_admin = new GraphAdmin());

		public GraphStorage()
			: this(new StorageEnvironment(StorageEnvironmentOptions.CreateMemoryOnly()), true)
		{
		}

		public GraphStorage(string path, string tempPath = null, string journalPath = null)
			: this(new StorageEnvironment(StorageEnvironmentOptions.ForPath(path, tempPath, journalPath)), true)
		{
		}

		public GraphStorage(StorageEnvironment env,bool ownsStorageEnvironment)
		{
			_env = env;
			CreateSchema();
			this._ownsStorageEnvironment = ownsStorageEnvironment;
			_byteStringContext = new ByteStringContext();
		}

		public Transaction ReadTransaction()
		{
			return new Transaction(this,_env.ReadTransaction());
		}

		public Transaction WriteTransaction()
		{
			return new Transaction(this,_env.WriteTransaction());
		}

		private void CreateSchema()
		{
			using (var tx = _env.WriteTransaction())
			{
				var edgeEtagNameByteString = _byteStringContext.From("EdgeEtag");
				_edgesSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = "EdgeEtag",
						NameAsSlice = new Slice(edgeEtagNameByteString),
						StartIndex = (int)EdgeTableFields.Etag,
						IsGlobal = true
					})
					.DefineIndex(FromToIndex.Name,FromToIndex);

				_edgesSchema.Create(tx, Constants.Schema.Edges);

				//for long-term system related storage
				var systemTree = tx.CreateTree(Constants.Schema.SystemDataTree);

				var vertexEtagByteString = _byteStringContext.From("VertexEtag");
				_verticesSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = "VertexEtag",
						NameAsSlice = new Slice(vertexEtagByteString),
						StartIndex = (int)VertexTableFields.Etag,
						IsGlobal = true
					});

				_verticesSchema.Create(tx, Constants.Schema.Vertices);

				tx.CreateTree(Constants.Schema.EtagToAdjacencyTree);
				tx.CreateTree(Constants.Schema.EtagToVertexTree);

				var fromToIndexByteString = _byteStringContext.From(nameof(FromToIndex));
				FromToIndex = new TableSchema.SchemaIndexDef
				{
					Name = nameof(FromToIndex),
					NameAsSlice = new Slice(fromToIndexByteString),
					StartIndex = (int)EdgeTableFields.FromKey,
					Count = 2,
					IsGlobal = true
				};

				if (systemTree.State.NumberOfEntries == 0)
				{
					//system data section -> for frequently accessed system data
					var systemDataSection = ActiveRawDataSmallSection.Create(tx.LowLevelTransaction, "Graph Storage");
					_systemDataSectionPage = systemDataSection.PageNumber;

					systemTree.Add(Constants.SystemKeys.GraphSystemDataPage, EndianBitConverter.Big.GetBytes(systemDataSection.PageNumber));

					//if fails to allocate several very small entries, we have a problem
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextVertexEtagEntry));
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextIdEntry));

					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextEdgeEtagEntry));

					systemDataSection.TryWriteInt64(_nextVertexEtagEntry, 1L);
					systemDataSection.TryWriteInt64(_nextIdEntry, 1L);

					systemDataSection.TryWriteInt64(_nextEdgeEtagEntry, 1L);

					systemTree.Add(Constants.SystemKeys.NextVertexEtagEntry, EndianBitConverter.Big.GetBytes(_nextVertexEtagEntry));
					systemTree.Add(Constants.SystemKeys.NextIdEntry, EndianBitConverter.Big.GetBytes(_nextIdEntry));
					systemTree.Add(Constants.SystemKeys.NextEdgeEtagEntry, EndianBitConverter.Big.GetBytes(_nextEdgeEtagEntry));
				}
				else
				{
					var res = systemTree.Read(Constants.SystemKeys.GraphSystemDataPage);
					_systemDataSectionPage = res.Reader.ReadBigEndianInt64();
					Debug.Assert(_systemDataSectionPage >= 0); //sanity check

					res = systemTree.Read(Constants.SystemKeys.NextVertexEtagEntry);
					_nextVertexEtagEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextIdEntry);
					_nextIdEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextEdgeEtagEntry);
					_nextEdgeEtagEntry = res.Reader.ReadBigEndianInt64();

				}
				tx.Commit();
			}
		}

		//TODO: refactor this to use a tree, since RawDataSection can be filled-out and refuse any writes
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal long NextValue(Transaction tx, IncrementingValue type)
		{
			if (tx.VoronTx.LowLevelTransaction.Flags != TransactionFlags.ReadWrite)
				throw new InvalidOperationException("Read/Write transaction expected");

			var entryId = TypeToEntryId(type);

			var val = tx.SystemDataSection.ReadInt64(entryId);
			Debug.Assert(tx.SystemDataSection.TryWriteInt64(entryId, val + 1));
			return val;
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

		private long TypeToEntryId(IncrementingValue type)
		{
			long entryId;
			switch (type)
			{
				case IncrementingValue.Id:
					entryId = _nextIdEntry;
					break;
				case IncrementingValue.VertexEtag:
					entryId = _nextVertexEtagEntry;
					break;
				case IncrementingValue.EdgeEtag:
					entryId = _nextEdgeEtagEntry;
					break;
				default:
					throw new InvalidOperationException("Invalid incrementing value type, don't know what to do...");
			}

			return entryId;
		}
	}
}
