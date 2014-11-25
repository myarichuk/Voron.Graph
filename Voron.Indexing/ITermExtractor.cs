using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Voron.Indexing
{
	public interface ITermExtractor
	{
		Dictionary<string,string[]> ExtractTermsByField(JObject data);
	}
}
