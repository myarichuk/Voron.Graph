using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Search
{
    public class NodeVisitedEventArgs : EventArgs
    {
        public Node VisitedNode { get; private set; }

        public Node PreviousNode { get; private set; }

        public uint TraversedEdgeCount { get; private set; }

        public NodeVisitedEventArgs(Node visitedNode, Node previousNode, uint traversedEdgeCount)
        {
            VisitedNode = visitedNode;
            PreviousNode = previousNode;
            TraversedEdgeCount = traversedEdgeCount;
        }
    }
}
