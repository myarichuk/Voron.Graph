using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Indexing.TermPostProcessors
{
	public class WhitespaceRemover : ITermPostProcessor
	{
		public WhitespaceRemover(int order)
		{
			Order = order;
		}

		public int Order { get; private set; }

		public string ProcessTerm(string term)
		{
			return term.Replace(" ", "");
		}
	}
}
