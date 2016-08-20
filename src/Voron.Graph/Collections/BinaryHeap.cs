using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Voron.Graph.Collections
{
	public class BinaryHeap<TKey,TValue> : IPriorityQueue<TKey, TValue>
		where TKey : IComparable<TKey>
	{
		//not a dictionary -> allows to have duplicate priorities (keys)
		private readonly List<HeapItem<TKey,TValue>> _items = new List<HeapItem<TKey, TValue>>();

		public bool IsEmpty => _items.Count == 0;

		public void ChangePriority(TKey priority, TKey newPriority)
		{
			if (_items.Count == 0)
				return;
			var index = _items.FindIndex(o => o.Priority.Equals(priority));
			if (index == -1)
				return;

			_items[index].Priority = newPriority;

			if (priority.CompareTo(newPriority) > 0)
				BubbleUp(index); //less priority -> need to bubble up, since lower priority should be first
			else
				BubbleDown(index);
		}

		public void Insert(TKey key,TValue data)
		{
			_items.Add(new HeapItem<TKey, TValue>(key,data));
			BubbleUp(_items.Count - 1);
		}

		public void DeleteMin()
		{
			if (_items.Count == 0)
				return;

			_items[0] = _items[_items.Count - 1];
			_items.RemoveAt(_items.Count - 1);
			if (_items.Count == 0)
				return;

			BubbleDown(0);
		}

		public TValue GetMin() => _items.Count == 0 ? default(TValue) : _items[0].Value;

		public TValue GetMinAndDelete()
		{
			if (_items.Count == 0)
				return default(TValue);

			var result = _items[0].Value;
			DeleteMin();
			return result;
		}	

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int LeftChildIndex(int index) => (2 * index) + 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int RightChildIndex(int index) => (2 * index) + 2;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ParentIndex(int index) => ((index - 1) / 2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsInRange(int index) => index >= 0 && index < _items.Count;

		private void BubbleDown(int index)
		{
			var left = LeftChildIndex(index);
			var right = RightChildIndex(index);

			var smallest = index;

			if (left < _items.Count && _items[left].Priority.CompareTo(_items[smallest].Priority) < 0)
				smallest = left;
			else if (right < _items.Count && _items[right].Priority.CompareTo(_items[smallest].Priority) < 0)
				smallest = right;				

			if (smallest != index)
			{
				var temp = _items[index];
				_items[index] = _items[smallest];
				_items[smallest] = temp;

				if ((IsInRange(left) && (_items[left].Priority.CompareTo(_items[smallest].Priority)) < 0 ||
					(IsInRange(right) && _items[right].Priority.CompareTo(_items[smallest].Priority) < 0)))
					BubbleDown(index);

				BubbleDown(smallest);
			}			
		}

		private void BubbleUp(int index)
		{
			var parentIndex = ParentIndex(index);
			if (!IsInRange(parentIndex))
				return;

			if (_items[parentIndex].Priority.CompareTo(_items[index].Priority) > 0)
			{
				var temp = _items[index];
				_items[index] = _items[parentIndex];
				_items[parentIndex] = temp;

				BubbleUp(parentIndex);
			}
		}

		
	}
}
