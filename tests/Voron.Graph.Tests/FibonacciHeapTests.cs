using System;
using Voron.Graph.Collections;

namespace Voron.Graph.Tests
{
	public class FibonacciHeapTests : BaseHeapTests
	{
		public override Func<IPriorityQueue<int,int>> CreateHeap => () => new FibonacciHeap<int,int>();
	}
}
