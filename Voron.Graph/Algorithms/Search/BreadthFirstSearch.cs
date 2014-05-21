using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Impl;

namespace Voron.Graph.Algorithms
{
    public class BreadthFirstSearch : BaseAlgorithm, IDisposable
    {
        private readonly GraphQueries _graphQueries;

        public BreadthFirstSearch(GraphQueries graphQueries, CancellationToken cancelToken)
            : base(cancelToken)
        {
            _graphQueries = graphQueries;
        }        

        public Task<Node> FindOne(Func<JObject,bool> searchPredicate)
        {
            var visitedNodes = new HashSet<long>();
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Node>> FindMany(Func<JObject, bool> searchPredicate)
        {
            var visitedNodes = new HashSet<long>();
            throw new NotImplementedException();
        }

        private Node GetRootNode(Transaction tx)
        {
            using(var iter = tx.NodeTree.Iterate(tx.VoronTransaction))
            {
                if (!iter.Seek(Slice.BeforeAllKeys))
                    return null;

                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(), resultStream.ToJObject());
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
