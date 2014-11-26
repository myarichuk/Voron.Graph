using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Voron.Indexing.Tokenizers
{
	public abstract class BaseTokenizer
	{
		protected IEnumerable<ITermValueFilter> TermFilters;

		protected BaseTokenizer(IEnumerable<ITermValueFilter> termFilters)
		{
			if (termFilters == null) throw new ArgumentNullException("termFilters");
			TermFilters = termFilters.ToList();
		}

		/// <summary>
		/// Extracts term tokens from json.net object field. The field should be primitive type - I.E do not contain child fields
		/// </summary>
		/// <exception cref="ArgumentException">Not supported for the field parameter to have child fields</exception>
		/// <returns>collection of tokens extracted from field value</returns>
		public IEnumerable<string> ExtractTerms(JValue field)
		{
			if (field.HasValues)
				throw new ArgumentException("Tokenizer should receive a primitive-type field - one without child fields","field");
			
			var processedFieldValue = TermFilters.Aggregate(field.Value.ToString(),
				(current, filter) => filter.ProcessTerm(current));

			return TermFromField(processedFieldValue);
		}

		protected abstract IEnumerable<string> TermFromField(string fieldValue);
	}
}