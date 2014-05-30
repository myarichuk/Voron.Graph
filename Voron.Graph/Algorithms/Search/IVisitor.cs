using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.Search
{
    public interface IVisitor
    {
        void DiscoverAdjacent(NodeWithEdge neighboorNode);

        //this is invoked when the node is popped/dequeued from processing queue
        void ExamineTraversal(TraversalNodeInfo traversalNodeInfo);

    }
}
