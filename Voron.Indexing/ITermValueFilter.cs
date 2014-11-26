using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Indexing
{
	public interface ITermValueFilter
	{
		int Order { get; }
		string ProcessTerm(string term);
	}
}
