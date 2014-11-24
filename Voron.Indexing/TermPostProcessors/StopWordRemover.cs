using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public string ProcessTerm(string term)
		{
			
		}
	}
}
