using Microsoft.VisualStudio.TestTools.UnitTesting;
using Voron.Graph.Algorithms.ShortestPath;

namespace Voron.Graph.Tests
{
	[TestClass]
    public class DijkstraShortestPathTests : BaseSingleDestinationShortestPathTests
    {
        protected override Algorithms.ShortestPath.ISingleDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode, Node targetNode)
        {
            return new DijkstraShortestPath(tx, graph, rootNode, targetNode, cancelTokenSource.Token);
        }
    }
}
