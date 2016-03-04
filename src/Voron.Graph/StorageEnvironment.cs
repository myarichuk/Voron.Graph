using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Tables;

namespace Voron.Graph
{
	public class StorageEnvironment : IDisposable
	{
		private bool isDisposed;
		private readonly Voron.StorageEnvironment _env;
		private TableSchema _adjacencyListSchema;

		public TableSchema AdjacencyListSchema => _adjacencyListSchema;

		public StorageEnvironment()
		{
			_env = new Voron.StorageEnvironment(StorageEnvironmentOptions.CreateMemoryOnly());
			CreateSchema();
		}

		public StorageEnvironment(string path, string tempPath = null, string journalPath = null)
		{
			_env = new Voron.StorageEnvironment(StorageEnvironmentOptions.ForPath(path,tempPath,journalPath));
			CreateSchema();
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
			if (!isDisposed)
			{
				_env.Dispose();
				isDisposed = true;
			}
			GC.SuppressFinalize(this);
		}

		~StorageEnvironment()
		{
			if (!isDisposed)
				Dispose();
		}		
	}
}
