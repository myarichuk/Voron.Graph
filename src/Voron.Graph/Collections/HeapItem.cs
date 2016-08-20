using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph.Collections
{
	public class HeapItem<TKey, TValue> where TKey : IComparable<TKey>
	{
		public TKey Priority { get; set; }

		public TValue Value { get; set; }

		public HeapItem(TKey priority, TValue value)
		{
			Priority = priority;
			Value = value;
		}
	}
}
