using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Voron.Graph.Collections
{
	public class BinaryMinHeap<T> : IMinHeap<T>
		where T : IComparable<T>
	{
		private readonly List<T> _items = new List<T>();

		public bool IsEmpty => _items.Count == 0;		

		public void Insert(T data)
		{
			_items.Add(data);
			BubbleUp(_items.Count - 1);
		}

		public void DeleteMin()
		{
			_items[0] = _items[_items.Count - 1];
			_items.RemoveAt(_items.Count - 1);
			if (_items.Count == 0)
				return;
			BubbleDown(0);
		}

		public T GetMin() => _items[0];

		public T GetMinAndDelete()
		{
			var result = _items[0];
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

			if (left < _items.Count && _items[left].CompareTo(_items[smallest]) < 0)
				smallest = left;
			else if (right < _items.Count && _items[right].CompareTo(_items[smallest]) < 0)
				smallest = right;

			if (smallest != index)
			{
				var temp = _items[index];
				_items[index] = _items[smallest];
				_items[smallest] = temp;

				if ((IsInRange(left) && (_items[left].CompareTo(_items[smallest])) < 0 ||
					(IsInRange(right) && _items[right].CompareTo(_items[smallest]) < 0)))
					BubbleDown(index);

				BubbleDown(smallest);
			}			
		}

		private void BubbleUp(int index)
		{
			var parentIndex = ParentIndex(index);
			if (!IsInRange(parentIndex))
				return;

			if (_items[parentIndex].CompareTo(_items[index]) > 0)
			{
				var temp = _items[index];
				_items[index] = _items[parentIndex];
				_items[parentIndex] = temp;

				BubbleUp(parentIndex);
			}
		}
	}
}
