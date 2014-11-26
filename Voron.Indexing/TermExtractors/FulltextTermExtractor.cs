using System.Collections.Generic;
using System.Linq;
using Voron.Indexing.TermFilters;

namespace Voron.Indexing.TermExtractors
{
	public class FulltextTermExtractor : BaseTermExtractor
	{
		private const int N = 3; //split the field values to trigrams

		public FulltextTermExtractor()
			: base(new ITermValueFilter[]
			{
				new LowercaseValueFilter(1), 
				new StopwordValueFilter(2),
				new WhitespaceValueFilter(3), 
				new DelimitersFilter(4), 
			})
		{
		}

		protected override IEnumerable<string> TermFromField(string fieldValue)
		{
			if (fieldValue.IsRegexMatch("^[0-9]+$") ||
				fieldValue.IsRegexMatch("(true|false)/i") || //i means regex is case insensitive
				fieldValue.Length == 1) //too small for breaking for terms
			{
				yield return fieldValue;
			}
			else
			{
				var currentIndex = 0;
				while (currentIndex < fieldValue.Length && fieldValue.Length - currentIndex >= 1)
				{
					var currentNgram = fieldValue.Skip(currentIndex).Take(FulltextTermExtractor.N);
					yield return new string(currentNgram.ToArray());
					currentIndex++;
				}
			}
		}	
	}
}
