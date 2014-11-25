using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Indexing.TermPostProcessors
{
	public class LowercaseConverter : ITermPostProcessor
	{
		public int Order { get; private set; }

		public LowercaseConverter(int order)
		{
			Order = order;
		}

		public string ProcessTerm(string term)
		{
			return term.ToLowerInvariant();
		}
	}
}
