using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Index;

namespace Voron.Graph.Indexing
{
	public class IndexingBatch : IDisposable
	{
		private readonly IndexWriter _writer;
		private readonly Index _parentIndex;

		public IndexingBatch(Index index, Analyzer analyzer = null)
		{
			_parentIndex = index;
			_writer = new IndexWriter(index.Directory, analyzer ?? new WhitespaceAnalyzer(), new IndexWriter.MaxFieldLength(Int16.MaxValue));
		}

		internal void Dispose(bool isDisposing)
		{
			_writer.Dispose(isDisposing);
			Interlocked.Exchange(ref _parentIndex._currentBatch, null);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		~IndexingBatch()
		{
			Dispose(false);
		}
	}
}
