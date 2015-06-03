using Microsoft.VisualStudio.TestTools.UnitTesting;
using Voron.Graph.Algorithms.ShortestPath;

namespace Voron.Graph.Tests
{
	[TestClass]
    public class BellmanFordMultiDestinationTests : BaseMultiDestinationShortestPathTests
    {     
        protected override IMultiDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode)
        {			
            return new BellmanFordMultiDestinationShortestPath(tx, graph, rootNode, cancelTokenSource.Token);
        }
    }
}
