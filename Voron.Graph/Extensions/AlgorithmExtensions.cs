using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Search;
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
            {
                var searchAlgorithm = new SearchAlgorithm(tx, storage, rootNode, algorithmType, cancelToken)
                {
                    SearchPredicate = searchPredicate,
                    ShouldStopSearch = results => take.HasValue ? results.Count() >= take : false
                };

                return searchAlgorithm.Search();
            }
        }

        public static async Task<IEnumerable<Node>> FindAsync(this GraphStorage storage,
            Node rootNode,
            Func<JObject, bool> searchPredicate,
            TraversalType algorithmType,
            CancellationToken cancelToken,
            int? take = null)
        {
            using (var tx = storage.NewTransaction(TransactionFlags.Read))
            {
                var searchAlgorithm = new SearchAlgorithm(tx, storage, rootNode, algorithmType, cancelToken)
                {
                    SearchPredicate = searchPredicate,
                    ShouldStopSearch = results => take.HasValue ? results.Count() >= take : false
                };

                return await searchAlgorithm.SearchAsync();
            }
        }
    }
}
