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

		public TableSchema AdjacencyListSchema => _adjacencyListSchema;

		private GraphAdvanced _advanced;
		public GraphAdvanced Advanced => (_advanced != null) ? _advanced : (_advanced = new GraphAdvanced(this));

		private GraphAdmin _admin;
		private long _lastEtagEntry;
		private long _nextVertexIdEntry;
		private long _systemDataSectionPage;

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
					.DefineIndex("ByFromKey",new TableSchema.SchemaIndexDef
					{
						Name = "FromKey",
						StartIndex = 1
					})
					.DefineIndex("ByToKey", new TableSchema.SchemaIndexDef
					{
						Name = "ToKey",
						StartIndex = 2
					});
				
				_adjacencyListSchema.Create(tx, Constants.Schema.AdjacencyList);		

				//for long-term system related storage
				var systemTree = tx.CreateTree(Constants.Schema.SystemDataTree);

				tx.CreateTree(Constants.Schema.VertexTree);
				tx.CreateTree(Constants.Schema.EtagToAdjacencyTree);
				tx.CreateTree(Constants.Schema.EtagToVertexTree);
				if (systemTree.State.NumberOfEntries == 0)
				{
					//system data section -> for frequently accessed system data
					var systemDataSection = ActiveRawDataSmallSection.Create(tx.LowLevelTransaction);
					_systemDataSectionPage = systemDataSection.PageNumber;

					systemTree.Add(Constants.SystemKeys.GraphSystemDataPage, EndianBitConverter.Big.GetBytes(systemDataSection.PageNumber));
					
					//if fails to allocate several very small entries, we have a problem
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _lastEtagEntry));
					Debug.Assert(systemDataSection.TryAllocate(sizeof(long), out _nextVertexIdEntry));

					systemDataSection.TryWriteInt64(_lastEtagEntry, 0L);
					systemDataSection.TryWriteInt64(_nextVertexIdEntry, 1L);

					systemTree.Add(Constants.SystemKeys.LastEtagEntry, EndianBitConverter.Big.GetBytes(_lastEtagEntry));
					systemTree.Add(Constants.SystemKeys.NextVertexIdEntry, EndianBitConverter.Big.GetBytes(_nextVertexIdEntry));
				}
				else
				{
					var res = systemTree.Read(Constants.SystemKeys.GraphSystemDataPage);
					_systemDataSectionPage = res.Reader.ReadBigEndianInt64();
					Debug.Assert(_systemDataSectionPage >= 0); //sanity check

					res = systemTree.Read(Constants.SystemKeys.LastEtagEntry);
					_lastEtagEntry = res.Reader.ReadBigEndianInt64();

					res = systemTree.Read(Constants.SystemKeys.NextVertexIdEntry);
					_nextVertexIdEntry = res.Reader.ReadBigEndianInt64();
				}
				tx.Commit();
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal long NextVertexId(Impl.Transaction tx)
		{
			if (tx.LowLevelTransaction.Flags != TransactionFlags.ReadWrite)
				throw new InvalidOperationException("Read/Write transaction expected");

			var systemDataSection = new ActiveRawDataSmallSection(tx.LowLevelTransaction, _systemDataSectionPage);
			var id = systemDataSection.ReadInt64(_nextVertexIdEntry);
			Debug.Assert(systemDataSection.TryWriteInt64(_nextVertexIdEntry, id + 1));
			return id;
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
	}
}
