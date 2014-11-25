using Voron.Indexing.TermFilters;
using Xunit;
using FluentAssertions;

namespace Voron.Indexing.Testing
{
	public class InfrastructureTests
	{
		[Fact]
		public void StopwordFilterShouldRemoveAllStopwords()
		{
			const string testString = @"This has gone too far. A FooBar is needed to restore all the honor that has been lost.Oh yeah? And who is going to stop me?! You and what army, eh?";
			const string expectedProcessedString = "has gone too far.  FooBar  needed  restore all  honor  has been lost.Oh yeah?  who  going  stop me?! You  what army, eh?";
			var stopwordsRemover = new StopwordFilter(1);

			var processedString = stopwordsRemover.ProcessTerm(testString);
			processedString.Should().Be(expectedProcessedString);
		}
	}
}
