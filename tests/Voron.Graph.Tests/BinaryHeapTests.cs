using System;
using Voron.Graph.Collections;

namespace Voron.Graph.Tests
{
	public class BinaryHeapTests : BaseHeapTests
	{
		public override Func<IPriorityQueue<int,int>> CreateHeap => () => new BinaryHeap<int,int>();
	}
}
