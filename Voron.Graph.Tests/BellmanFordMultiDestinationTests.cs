using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.ShortestPath;
using Voron.Graph.Extensions;
using FluentAssertions;
using Voron.Graph.Exceptions;

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
