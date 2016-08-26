using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph.Collections
{
	public struct HeapItem<TKey, TValue> where TKey : IComparable<TKey>
	{
		public TKey Priority;

		public TValue Value;

		public HeapItem(TKey priority, TValue value)
		{
			Priority = priority;
			Value = value;
		}
	}
}
