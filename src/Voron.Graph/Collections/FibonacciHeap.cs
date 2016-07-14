using System;
using System.Collections.Generic;

namespace Voron.Graph.Collections
{
	public class FibonacciHeap<T> : IPriorityQueue<T>
		where T : IComparable<T>
	{
		private readonly LinkedList<T> _roots = new LinkedList<T>();
		private int _count;

		public bool IsEmpty => _count == 0;

		public void Insert(T data)
		{
			throw new NotImplementedException();
		}

		public void DeleteMin()
		{
			throw new NotImplementedException();
		}

		public T GetMin()
		{
			throw new NotImplementedException();
		}

		public T GetMinAndDelete()
		{
			throw new NotImplementedException();
		}

	
	}
}
