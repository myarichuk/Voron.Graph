using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Mapping;

namespace Voron.Graph.Indexing
{
	public class IndexBatch : IDisposable
	{
		private readonly IndexWriter _writer;
		private readonly MappingSettings _mappingSettings;

		public IndexBatch(IndexWriter writer)
		{
			_writer = writer;
		}

		public void Add<T>(T indexItem) where T : class
		{
			var doc = indexItem.ToDocument();			
			_writer.AddDocument(doc);
		}

		public void Dispose()
		{
			_writer.Flush(true, false, true);
		}
	}
}
