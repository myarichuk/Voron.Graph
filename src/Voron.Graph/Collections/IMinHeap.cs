using System;

namespace Voron.Graph.Collections
{
	public interface IMinHeap<T>
		where T : IComparable<T>
	{
		void Insert(T data);
		void DeleteMin();

		bool IsEmpty { get; }

		T GetMin();
		T GetMinAndDelete();
	}
}
