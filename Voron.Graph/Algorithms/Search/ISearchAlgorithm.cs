using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Impl;

namespace Voron.Graph.Algorithms.Search
{
    public interface ISearchAlgorithm
    {
        Task<Node> FindOne(Transaction tx, Func<JObject, bool> searchPredicate);

        Task<List<Node>> FindMany(Transaction tx, Func<JObject, bool> searchPredicate);

        event Action<Node> NodeVisited;

        event Action<Node> NodeFound;
    }
}
