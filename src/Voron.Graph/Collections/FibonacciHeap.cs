using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph.Collections
{
	public class FibonacciHeap<T>
	{
		public class Node
		{
			public T Data { get; set; }

			public Node Next { get; set; }
			public Node Prev { get; set; }
		}
	}
}
