using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Voron.Graph.Indexing;
using FluentAssertions;

namespace Voron.Graph.Tests
{
	[TestClass]
	public class IndexingTests
	{
		private readonly string dataFolder;

		public IndexingTests()
		{
			dataFolder = new FileInfo(this.GetType().Assembly.Location).Directory.FullName;
			dataFolder += (Path.DirectorySeparatorChar + "Data");
		}

		[TestMethod]
		public void Should_be_able_to_create_multiple_batches_sequentially()
		{
			using (var index = new Index(dataFolder, true))
			{
				index.Invoking(x =>
				{
					using (x.Batch())
					{
					}
					using (x.Batch())
					{
					}
					using (x.Batch())
					{
					}
				}).ShouldNotThrow();
			}
		}

		[TestMethod]
		public void Should_not_be_able_to_create_two_indexing_batches_concurrently()
		{
			using (var index = new Index(dataFolder, true))
			using (index.Batch())
			{
				index.Invoking(x =>
				{
					using (x.Batch())
					{
					}
				}).ShouldThrow<Exception>();
			}
		}
	}
}
