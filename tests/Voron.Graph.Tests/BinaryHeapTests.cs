using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Graph.Collections;

namespace Voron.Graph.Tests
{
	public class BinaryHeapTests : BaseHeapTests
	{
		public override Func<IPriorityQueue<int>> CreateHeap => () => new BinaryHeap<int>();
	}
}
