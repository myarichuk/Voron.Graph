using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public partial class GraphStorage : IDisposable
	{
		private readonly bool _ownsStorageEnvironment;
		private bool _isDisposed;
		private readonly StorageEnvironment _env;
		private TableSchema _adjacencyListSchema;

		public TableSchema AdjacencyListSchema => _adjacencyListSchema;

		private GraphAdvanced _advanced;
		public GraphAdvanced Advanced => (_advanced != null) ? _advanced : (_advanced = new GraphAdvanced(this));

		private GraphAdmin _admin;
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
						Name = "ByEtag",
						StartIndex = 0
					})
					.DefineIndex("ByFromKey",new TableSchema.SchemaIndexDef
					{
						Name = "ByFromKey",
						StartIndex = 1
					})
					.DefineIndex("ByToKey", new TableSchema.SchemaIndexDef
					{
						Name = "ByToKey",
						StartIndex = 2
					});

				_adjacencyListSchema.Create(tx, Constants.Schema.AdjacencyList);		
				tx.CreateTree(Constants.Schema.VertexTree);
				tx.CreateTree(Constants.Schema.EtagToAdjacencyTree);
				tx.CreateTree(Constants.Schema.EtagToVertexTree);
				tx.Commit();
			}
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
