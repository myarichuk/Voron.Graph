using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Search;

namespace Voron.Graph.Extensions
{
    public static class AlgorithmExtensions
    {
        public static async Task<Node> FindOne(this ISearchAlgorithm searchAlgorithm, Transaction tx, Func<JObject, bool> searchPredicate)
        {
            Node result = null;
            Func<bool> shouldStopFunc = () => result != null;
            searchAlgorithm.NodeFound += foundNode => result = foundNode;

            await searchAlgorithm.Traverse(tx, searchPredicate, shouldStopFunc);

            return result;
        }

        public static async Task<List<Node>> FindMany(this ISearchAlgorithm searchAlgorithm, Transaction tx, Func<JObject, bool> searchPredicate)
        {
            var results = new List<Node>();
            searchAlgorithm.NodeFound += results.Add;

            await searchAlgorithm.Traverse(tx, searchPredicate, () => false);

            return results;
        }

    }
}
