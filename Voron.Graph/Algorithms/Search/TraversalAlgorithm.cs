using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Search
{
    public enum TraversalType
    {
        BFS,
        DFS
    }

    public class TraversalAlgorithm : BaseRootedAlgorithm, ISearchAlgorithm
    {
        private TraversalType _traversalType;

        public TraversalAlgorithm(TraversalType traversalType, CancellationToken cancelToken)
            : base(cancelToken)
        {
            _traversalType = traversalType;
        }


        protected override Node GetDefaultRootNode(Transaction tx)
        {
            using (var iter = tx.NodeTree.Iterate())
            {
                if (!iter.Seek(Slice.BeforeAllKeys))
                    return null;

                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
                {
                    Etag etag;
                    JObject value;
                    Util.EtagAndValueFromStream(resultStream, out etag, out value);
                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag);
                }
            }
        }

        public Task Traverse(Transaction tx, 
            Func<JObject, bool> searchPredicate, 
            Func<bool> shouldStopPredicate, 
            Node rootNode = null, 
            ushort? edgeTypeFilter = null, 
            uint? traverseDepthLimit = null)
        {
            if (State == AlgorithmState.Running)
                throw new InvalidOperationException("The algorithm is already running");
            throw new NotImplementedException();
        }

        public event Action<NodeVisitedEventArgs> NodeVisited;

        protected void OnNodeVisited(NodeVisitedEventArgs node)
        {
            var nodeVisited = NodeVisited;
            if (nodeVisited != null)
                nodeVisited(node);
        }

        public event Action<Node> NodeFound;

        protected void OnNodeFound(Node node)
        {
            var nodeFound = NodeFound;
            if (nodeFound != null)
                nodeFound(node);
        }

        //private interface ITraversalStorage<T>
        //{
        //    T GetNext();
        //    Put(T 
        //}
    }
}
