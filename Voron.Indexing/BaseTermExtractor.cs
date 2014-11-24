using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Indexing
{
	public abstract class BaseTermExtractor<T>
		where T : class
	{
		protected readonly ITermPostProcessor[] PostProcessors;

		protected BaseTermExtractor(IEnumerable<ITermPostProcessor> postProcessors)
		{
			PostProcessors = postProcessors.ToArray();
		}

		protected abstract IEnumerable<string> ExtractTermsFrom(T termsObject);

		public IEnumerable<string> ExtractTerms(T termsObject)
		{
			var terms = ExtractTermsFrom(termsObject);
			return terms.Select(term => PostProcessors.Aggregate(term, (current, postProcessor) => 
													postProcessor.ProcessTerm(current)));
		}
	}
}
