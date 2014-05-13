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
        Edge CreateEdge(Node nodeFrom,Node nodeTo, Stream value = null);

        void Delete(Node node);
        void Delete(Edge edge);

        Stream GetValueOf(Node node);
        Stream GetValueOf(Edge edge);

        void SaveChanges();

        IEnumerable<Node> GetAdjacentOf(Node node);

        bool IsIsolated(Node node);
        
        Iterator<Edge> IterateEdges();
        Iterator<Node> IterateNodes();
    }
}
