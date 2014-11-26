using System.Collections.Generic;
using System.Linq;
using Voron.Indexing.TermFilters;

namespace Voron.Indexing.Tokenizers
{
	public class NGramTokenizer : BaseTokenizer
	{
		private readonly int _n = 3; //split the field values to trigrams

		public NGramTokenizer(int n = 3)
			: base(new ITermValueFilter[]
			{
				new LowercaseValueFilter(1), 
				new StopwordValueFilter(2),
				new WhitespaceValueFilter(3), 
				new DelimitersFilter(4), 
			})
		{
			_n = n;
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
					var currentNgram = fieldValue.Skip(currentIndex).Take(_n);
					yield return new string(currentNgram.ToArray());
					currentIndex++;
				}
			}
		}	
	}
}
