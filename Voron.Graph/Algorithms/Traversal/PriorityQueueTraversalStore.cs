using C5;
using System.Collections.Generic;

namespace Voron.Graph.Algorithms.Traversal
{
	public class PriorityQueueTraversalStore : INodeTraversalStore<TraversalNodeInfo>
    {
        private readonly IPriorityQueue<TraversalNodeInfo> _store;

        public PriorityQueueTraversalStore(IComparer<TraversalNodeInfo> comparer)
        {
            _store = new IntervalHeap<TraversalNodeInfo>(comparer);
        }

        public TraversalNodeInfo GetNext()
        {
            return _store.DeleteMin();
        }

        public void Put(TraversalNodeInfo item)
        {
            _store.Add(item);
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public IEnumerator<TraversalNodeInfo> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _store.GetEnumerator();
        }
    }
}
