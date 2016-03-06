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

		private TableSchema _adjacencyListSchema;
		private TableSchema _verticesSchema;

		internal TableSchema AdjacencyListSchema => _adjacencyListSchema;
		internal TableSchema VerticesSchema => _verticesSchema;

		private GraphAdvanced _advanced;
		public GraphAdvanced Advanced => (_advanced != null) ? _advanced : (_advanced = new GraphAdvanced(this));

		private GraphAdmin _admin;
		private long _nextEdgeIdEntry;
		private long _nextEdgeEtagEntry;
		private long _nextVertexEtagEntry;
		private long _nextVertexIdEntry;
		private long _systemDataSectionPage;

		internal long SystemDataSectionPage => _systemDataSectionPage;

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
				_adjacencyListSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = "Etag",
						StartIndex = 0
					})
					.DefineIndex("FromId",new TableSchema.SchemaIndexDef
					{
						Name = "FromId",
						StartIndex = 1
					})
					.DefineIndex("ToId", new TableSchema.SchemaIndexDef
					{
						Name = "ToId",
						StartIndex = 2
					});
				
				_adjacencyListSchema.Create(tx, Constants.Schema.AdjacencyList);		

				//for long-term system related storage
				var systemTree = tx.CreateTree(Constants.Schema.SystemDataTree);

				_verticesSchema = new TableSchema()
					.DefineKey(new TableSchema.SchemaIndexDef
					{
						Name = "Id",
						StartIndex = 0
					})
					.DefineIndex("Etag", new TableSchema.SchemaIndexDef
					{
						Name = "Etag",
						StartIndex = 1
					});
				_verticesSchema.Create(tx,Constants.Schema.Vertices);

				tx.CreateTree(Constants.Schema.EtagToAdjacencyTree);
				tx.CreateTree(Constants.Schema.EtagToVertexTree);

				if (systemTree.State.NumberOfEntries == 0)
				{
					//system data section -> for frequently accessed system data
					var systemDataSection = ActiveRawDataSmallSection.Create(tx.LowLevelTransaction);
					_systemDataSectionPage = systemDataSection.PageNumber;

					systemTree.Add(Constants.SystemKeys.GraphSystemDataPage, EndianBitConverter.Big.GetBytes(systemDataSection.PageNumber));

					//if fails to allocate several very small entries, we have a problem
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextVertexEtagEntry));
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextVertexIdEntry));

					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextEdgeEtagEntry));
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextEdgeIdEntry));

					systemDataSection.TryWriteInt64(_nextVertexEtagEntry, 1L);
					systemDataSection.TryWriteInt64(_nextVertexIdEntry, 1L);

					systemDataSection.TryWriteInt64(_nextEdgeEtagEntry, 1L);
					systemDataSection.TryWriteInt64(_nextEdgeIdEntry, 1L);

					systemTree.Add(Constants.SystemKeys.NextVertexEtagEntry, EndianBitConverter.Big.GetBytes(_nextVertexEtagEntry));
					systemTree.Add(Constants.SystemKeys.NextVertexIdEntry, EndianBitConverter.Big.GetBytes(_nextVertexIdEntry));

					systemTree.Add(Constants.SystemKeys.NextEdgeEtagEntry, EndianBitConverter.Big.GetBytes(_nextEdgeEtagEntry));
					systemTree.Add(Constants.SystemKeys.NextEdgeIdEntry, EndianBitConverter.Big.GetBytes(_nextEdgeIdEntry));
				}
				else
				{
					var res = systemTree.Read(Constants.SystemKeys.GraphSystemDataPage);
					_systemDataSectionPage = res.Reader.ReadBigEndianInt64();
					Debug.Assert(_systemDataSectionPage >= 0); //sanity check

					res = systemTree.Read(Constants.SystemKeys.NextVertexEtagEntry);
					_nextVertexEtagEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextVertexIdEntry);
					_nextVertexIdEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextEdgeEtagEntry);
					_nextEdgeEtagEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextEdgeIdEntry);
					_nextEdgeIdEntry = res.Reader.ReadBigEndianInt64();
				}
				tx.Commit();
			}
		}

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
				case IncrementingValue.VertexId:
					entryId = _nextVertexIdEntry;
					break;
				case IncrementingValue.VertexEtag:
					entryId = _nextVertexEtagEntry;
					break;
				case IncrementingValue.EdgeId:
					entryId = _nextEdgeIdEntry;
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
