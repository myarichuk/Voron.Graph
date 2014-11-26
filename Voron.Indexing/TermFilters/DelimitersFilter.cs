using System;
using System.Text.RegularExpressions;

namespace Voron.Indexing.TermFilters
{
	public class DelimitersFilter : ITermValueFilter
	{
		//delimiter text taken from here
		//http://nlpdotnet.com/SampleCode/TokenizeTextRuthlessly.aspx
		private readonly string[] _delimiters = {
				"{","}","(",")","[","]",">","<","-","_","=","+",
				"|","\\",":",";","","\"",",",".","/","?","~","!",
				"@","#","$","%","^","&","*","","\r","\n","\t"};


		private Regex _delimiterRegex;

		public int Order { get; private set; }

		public DelimitersFilter(int order)
		{
			Order = order;
		}

		public string ProcessTerm(string term)
		{
			if (_delimiterRegex == null)
			{
				var delimiterRegexExpression = string.Format("({0})", String.Join("|", _delimiters));
				_delimiterRegex = new Regex(delimiterRegexExpression,RegexOptions.Compiled | RegexOptions.CultureInvariant);
			}

			return _delimiterRegex.Replace(term, "");
		}
	}
}
