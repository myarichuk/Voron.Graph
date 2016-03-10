using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph
{
	public partial class GraphStorage
	{
		public TraversalBuilder Traverse() => new TraversalBuilder(this);

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
