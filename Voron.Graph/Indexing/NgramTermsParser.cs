using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Voron.Graph.Indexing
{
	public class NgramTermsParser : ITermsParser
	{
		private readonly int n;

		public NgramTermsParser(int n)
		{
			this.n = n;
		}

		public int MinimumTermSize
		{
			get { return n; }
		}



		public IEnumerable<string> GetTerms(JValue token)
		{
			return GetTerms(token.Value.ToString());
		}

		public IEnumerable<string> GetTerms(string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				yield break;

			int currentIndex = 0;
			value = value.ToLower();
			
			//ignore numbers and booleans -> treat them as single term
			if (value.IsRegexMatch("^[0-9]+$") ||
			    value.IsRegexMatch("(true|false)/i") ||//i means regex is case insensitive
			    value.Length == 1) //too small for breaking for terms
			{
				yield return value;
			}
			else
			{
				while (currentIndex < value.Length && value.Length - currentIndex >= 1)
				{
					yield return new string(value.Skip(currentIndex).Take(n).ToArray());
					currentIndex++;
				}
			}
		}
	}
}
