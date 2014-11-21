using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Impl;
using Voron.Graph.Indexing;

namespace Voron.Graph.Tests
{
	[TestClass]
	public class IndexingTests : BaseGraphTest
	{
		[TestMethod]
		public void TrigramTermsParserWorksProperly()
		{
			var termsParser = new NgramTermsParser(3);
			var terms = termsParser.GetTerms("abcde").ToList();

			terms.Should().Contain(new[] {"abc", "bcd", "cde", "de", "e"})
						  .And.HaveCount(5);
		}

		[TestMethod]
		public void NodeIndexWillStoreData()
		{
			var graph = new GraphStorage("test", Env);
			
			graph.AddIndexedProperties("Data"); //set the Data property as indexed
			var nodeIndex = new NodeIndex(graph, new NgramTermsParser(3));

			var node1 = new Node(1, JObject.FromObject(new { Data = "abcde" , JustAProperty = 111}));
			var node2 = new Node(2, JObject.FromObject(new { Data = "defgh" }));
			var node3 = new Node(3, JObject.FromObject(new { Data = "efghk" }));
			var node4 = new Node(4, JObject.FromObject(new { Data = 1234 }));
			
			nodeIndex.IndexDataIfRelevant(node1);
			nodeIndex.IndexDataIfRelevant(node2);
			nodeIndex.IndexDataIfRelevant(node3);
			nodeIndex.IndexDataIfRelevant(node4);

			var results1 = nodeIndex.Query("efg").ToList();
			var results2 = nodeIndex.Query("efg").ToList();

			results1.Should().OnlyContain(id => id == 2 || id == 3)
							 .And.HaveCount(2);

			results2.Should().OnlyContain(id => id == 2 || id == 3)
							 .And.HaveCount(2);

			var results3 = nodeIndex.Query("e").ToList(); //kind-of edge use-case

			results3.Should().OnlyContain(id => id == 1 || id == 2 || id == 3)
							 .And.HaveCount(3);

			var results4 = nodeIndex.Query("fgh").ToList();
			results4.Should().OnlyContain(id => id == 2 || id == 3)
							 .And.HaveCount(2);
		}
	}
}
