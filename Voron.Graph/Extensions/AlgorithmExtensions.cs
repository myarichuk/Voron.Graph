using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;
using System.Linq;

namespace Voron.Graph.Extensions
{
    public static class AlgorithmExtensions
    {
        public static IEnumerable<Node> Find(this GraphStorage storage, 
            Node rootNode, 
            Func<JObject,bool> searchPredicate,
            TraversalType algorithmType,             
            CancellationToken cancelToken,
            int? take = null)
        {
            using(var tx = storage.NewTransaction(TransactionFlags.Read))
                return storage.Find(tx, rootNode, searchPredicate, algorithmType, cancelToken, take);
        }

        public static IEnumerable<Node> Find(this GraphStorage storage,
            Transaction tx,
            Node rootNode,
            Func<JObject, bool> searchPredicate,
            TraversalType algorithmType,
            CancellationToken cancelToken,
            int? take = null)
        {
            var searchVisitor = new SearchVisitor(searchPredicate, take ?? 0);
            var traversalAlgorithm = new TraversalAlgorithm(tx, storage, rootNode, algorithmType, cancelToken)
            {
               Visitor = searchVisitor
            };

            traversalAlgorithm.Traverse();
            return searchVisitor.Results;
        }

        public static async Task<IEnumerable<Node>> FindAsync(this GraphStorage storage,
            Node rootNode,
            Func<JObject, bool> searchPredicate,
            TraversalType algorithmType,
            CancellationToken cancelToken,
            int? take = null)
        {
            using (var tx = storage.NewTransaction(TransactionFlags.Read))
                return await storage.FindAsync(tx, rootNode, searchPredicate, algorithmType, cancelToken, take);
        }

        public static async Task<IEnumerable<Node>> FindAsync(this GraphStorage storage,
                 Transaction tx,
                 Node rootNode,
                 Func<JObject, bool> searchPredicate,
                 TraversalType algorithmType,
                 CancellationToken cancelToken,
                 int? take = null)
        {
            var searchVisitor = new SearchVisitor(searchPredicate, take ?? 0);
            var traversalAlgorithm = new TraversalAlgorithm(tx, storage, rootNode, algorithmType, cancelToken)
            {
                Visitor = searchVisitor
            };

            await traversalAlgorithm.TraverseAsync();
            return searchVisitor.Results;
        }
    }
}
