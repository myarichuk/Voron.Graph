using System;
using System.Collections.Generic;

namespace Voron.Graph.Collections
{
	public class FibonacciHeap<TKey, TValue> : IPriorityQueue<TKey, TValue>
		where TKey : IComparable<TKey>
	{
		public bool IsEmpty
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void ChangePriority(TKey key, TKey amount)
		{
			throw new NotImplementedException();
		}

		public void DeleteMin()
		{
			throw new NotImplementedException();
		}

		public TValue GetMin()
		{
			throw new NotImplementedException();
		}

		public TValue GetMinAndDelete()
		{
			throw new NotImplementedException();
		}

		public void Insert(TKey key, TValue data)
		{
			throw new NotImplementedException();
		}
	}
}
