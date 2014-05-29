using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Primitives
{
    public class NodeWithEdge
    {
        public Node Node { get; set; }

        public Edge EdgeTo { get; set; }
    }
}
