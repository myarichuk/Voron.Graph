using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public interface ISession : IDisposable
    {
        Node CreateNode(Stream value);
        Edge CreateEdgeBetween(Node nodeFrom,Node nodeTo, Stream value = null);

        void Delete(Node node);
        void Delete(Edge edge);

        Node NodeByKey(long nodeKey);
        IEnumerable<Edge> GetEdgesBetween(Node nodeFrom, Node nodeTo, Func<ushort, bool> edgeTypePredicate = null);

        void SaveChanges();

        IEnumerable<Node> GetAdjacentOf(Node node, Func<ushort, bool> edgeTypePredicate = null);

        bool IsIsolated(Node node);
        
        Iterator<Edge> IterateEdges();
        Iterator<Node> IterateNodes();
    }
}
