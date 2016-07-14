using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voron.Graph.Collections;

namespace Voron.Graph.Tests
{
	public class FibonacciHeapTests : BaseHeapTests
	{
		public override Func<IPriorityQueue<int>> CreateHeap => () => new FibonacciHeap<int>();
	}
}
