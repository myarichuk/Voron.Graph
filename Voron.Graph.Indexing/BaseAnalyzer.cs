using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Indexing
{
	public abstract class BaseAnalyzer
	{
		protected readonly List<IPreprocessor> Preprocessors;

		protected BaseAnalyzer(IEnumerable<IPreprocessor> preprocessors)
		{
			if (preprocessors == null) throw new ArgumentNullException("preprocessors");

			Preprocessors = preprocessors.ToList();
		}

		public abstract IEnumerable<string> GetTerms(Newtonsoft.Json.Linq.JValue token);

		public abstract IEnumerable<string> GetTerms(string value);
	}
}
