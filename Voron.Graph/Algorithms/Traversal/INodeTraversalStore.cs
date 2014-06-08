using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Traversal
{
    public interface INodeTraversalStore<T> : IEnumerable<T>
    {
        T GetNext();
        void Put(T item);

        int Count { get; }
    }
}
