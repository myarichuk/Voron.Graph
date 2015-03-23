using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Voron.Graph.Indexing
{
	public class Index : IDisposable
	{
		private readonly Lucene.Net.Store.Directory _indexDirectory;
		internal IndexWriter _writer;

		internal Lucene.Net.Store.Directory Directory
		{
			get
			{
				return _indexDirectory;
			}
		}

		private readonly string _indexPath;

		public Index(string indexPath,bool runInMemory = false)
		{
			_indexPath = indexPath;
			_indexDirectory = runInMemory ? (Lucene.Net.Store.Directory)(new RAMDirectory()) : 
											(Lucene.Net.Store.Directory)(new MMapDirectory(new DirectoryInfo(indexPath)));
			
		}

		public IndexBatch Batch()
		{
			return new IndexBatch(_writer);
		}

		private void Dispose(bool isDisposing)
		{
			_writer.Dispose(isDisposing);
			_indexDirectory.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Index()
		{
			Dispose(false);
		}
	}
}
