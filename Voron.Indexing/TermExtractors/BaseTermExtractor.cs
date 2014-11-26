using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Voron.Indexing.TermExtractors
{
	public abstract class BaseTermExtractor
	{
		protected IEnumerable<ITermValueFilter> TermFilters;

		protected BaseTermExtractor(IEnumerable<ITermValueFilter> termFilters)
		{
			if (termFilters == null) throw new ArgumentNullException("termFilters");
			TermFilters = termFilters.ToList();
		}

		public IEnumerable<string> ExtractTerms(JValue field)
		{
			Debug.Assert(field.HasValues == false," Term Extractor should receive a primitive field - one without child fields");
			
			var processedFieldValue = TermFilters.Aggregate(field.Value.ToString(),
				(current, filter) => filter.ProcessTerm(current));

			return TermFromField(processedFieldValue);
		}

		protected abstract IEnumerable<string> TermFromField(string fieldValue);
	}
}