using System;
using System.Collections.Generic;

namespace Voron.Graph.Collections
{
	public class FibonacciHeap<TKey, TValue> : IPriorityQueue<TKey, TValue>
		where TKey : IComparable<TKey>
	{
		private readonly List<List<HeapItem<TKey, TValue>>> _roots = new List<List<HeapItem<TKey, TValue>>> { new List<HeapItem<TKey, TValue>>() };

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
