using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Search
{
    public interface ISearchAlgorithm
    {
        Task Traverse(Transaction tx, 
            Func<JObject, bool> searchPredicate, 
            Func<bool> shouldStopPredicate, 
            Node rootNode = null,
            ushort? edgeTypeFilter = null,
            uint? traverseDepthLimit = null);

        event Action<NodeVisitedEventArgs> NodeVisited;

        event Action<Node> NodeFound;
    }
}
