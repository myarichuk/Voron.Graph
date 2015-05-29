using System.Collections.Generic;

namespace Voron.Graph.Algorithms.Traversal
{
	public interface INodeTraversalStore<T> : IEnumerable<T>
    {
        T GetNext();
        void Put(T item);

        int Count { get; }
    }
}
