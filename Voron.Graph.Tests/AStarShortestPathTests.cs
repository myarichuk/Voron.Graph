using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.ShortestPath;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class AStarShortestPathTests : BaseSingleDestinationShortestPathTests
    {
        protected override ISingleDestinationShortestPath GetAlgorithm(Transaction tx, GraphStorage graph, Node rootNode,Node targetNode)
        {
            return new AStarShortestPath(tx, graph, rootNode, targetNode, 
                (nodeFrom, nodeTo) => 
                {
                    //eucledean distance
                    var nodeToLocation = nodeLocations[nodeTo.Key];
                    var nodeFromLocation = nodeLocations[nodeFrom.Key];

                    return Math.Sqrt(Math.Pow(nodeToLocation.y - nodeFromLocation.y, 2) + Math.Pow(nodeToLocation.x - nodeFromLocation.x, 2));
                }, cancelTokenSource.Token);
        }
    }
}
