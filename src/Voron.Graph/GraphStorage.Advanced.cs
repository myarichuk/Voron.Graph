using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph
{
	public partial class GraphStorage
	{
		public class GraphAdvanced
		{
			private readonly GraphStorage _parent;

			internal GraphAdvanced(GraphStorage parent)
			{
				_parent = parent;
			}
		}
	}
}
