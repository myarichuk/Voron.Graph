using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Voron.Graph.Indexing
{
	public interface IAnalyzer
	{
		IEnumerable<string> GetTerms(JValue token);

		IEnumerable<string> GetTerms(string value);
	}
}
