using System;

namespace Voron.Graph.Collections
{
	public interface IPriorityQueue<TKey,TValue>
		where TKey : IComparable<TKey>
	{
		void Insert(TKey key,TValue data);
		void DeleteMin();
		void ChangePriority(TKey priority, TKey newPriority);

		bool IsEmpty { get; }

		TValue GetMin();
		TValue GetMinAndDelete();
	}
}
