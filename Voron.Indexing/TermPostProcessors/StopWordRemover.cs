using System.Text.RegularExpressions;
using System.Threading;

namespace Voron.Indexing.TermPostProcessors
{
	public class StopwordRemover : ITermPostProcessor
	{
		private readonly string[] _stopwords = new[]
		{
			"a",
			"an",
			"and",
			"are",
			"as",
			"at",
			"be",
			"but",
			"by",
			"for",
			"if",
			"in",
			"into",
			"is",
			"it",
			"no",
			"not",
			"of",
			"on",
			"or",
			"such",
			"that",
			"the",
			"their",
			"then",
			"there",
			"these",
			"they",
			"this",
			"to",
			"was",
			"will",
			"with"
		};

		private Regex _stopWordMatchingRegex;

		public int Order { get; private set; }

		public StopwordRemover(int order)
		{
			Order = order;
		}

		public string ProcessTerm(string term)
		{
			//regex taken from http://lotsacode.wordpress.com/2010/10/08/remove-google-stopwords-from-string/
			//I suck at regexes, other than the most basic ones..

			if (_stopWordMatchingRegex == null)
			{
				var stopwordRegex =
					@"(?<=(\A|\s|\.|,|!|\?))(" +
					string.Join("|", _stopwords) +
					@")(?=(\s|\z|\.|,|!|\?))";

				Interlocked.Exchange(ref _stopWordMatchingRegex,
					new Regex(stopwordRegex, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled));
			}
			return _stopWordMatchingRegex.Replace(term, "");
		}
	}
}
