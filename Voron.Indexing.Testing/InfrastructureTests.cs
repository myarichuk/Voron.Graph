using Voron.Indexing.TermFilters;
using Xunit;
using FluentAssertions;
using Voron.Indexing.Tokenizers;

namespace Voron.Indexing.Testing
{
	public class InfrastructureTests
	{
		[Fact]
		public void StopwordFilterShouldRemoveAllStopwords()
		{
			const string testString = @"This has gone too far. A FooBar is needed to restore all the honor that has been lost.Oh yeah? And who is going to stop me?! You and what army, eh?";
			const string expectedProcessedString = " has gone too far.  FooBar  needed  restore all  honor  has been lost.Oh yeah?  who  going  stop me?! You  what army, eh?";
			var stopwordsRemover = new StopwordValueFilter(1);
			
			var processedString = stopwordsRemover.ProcessTerm(testString);
			processedString.Should().Be(expectedProcessedString);
		}

		[Fact]
		public void Can_persist_stored_field_names()
		{
            using (var env = new StorageEnvironment(StorageEnvironmentOptions.ForPath("\\")))
            {
                var index = new Index("TestIndex",env,new NGramTokenizer());
                index.AddIndexedField("foo");
                index.AddIndexedField("bar");
                index.AddIndexedField("baa");
            }
		}
	}
}
