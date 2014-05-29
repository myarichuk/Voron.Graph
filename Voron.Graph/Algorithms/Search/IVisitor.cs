using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.Search
{
    public interface IVisitor
    {
        //this is invoked when node is encountered for the first time
        void DiscoverNode(Node node);

        //this is invoked when edge that leads to discovered node is encountered for the first time
        void DiscoverEdge(Edge edge);

        //this is invoked when the node is popped/dequeued from processing queue
        void ExamineTraversal(TraversalNodeInfo traversalNodeInfo);

    }
}
