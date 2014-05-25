using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Extensions
{
    //alternative syntax for doing stuff with nodes
    public static class NodeExtensions
    {
        public static Edge ConnectWith(this Node node,Transaction tx,Node otherNode, GraphStorage storage)
        {            
            return storage.Commands.CreateEdgeBetween(tx,node,otherNode);
        }

        public static Task<List<Edge>> GetEdges(this Node node, Transaction tx, GraphStorage storage, CancellationToken token)
        {
            return storage.AdminQueries.GetEdgesOfNode(tx, node, token);
        }
    }
}
