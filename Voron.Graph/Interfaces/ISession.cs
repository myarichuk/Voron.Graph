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
        void PutNode(string nodeKey, Stream value);
        void PutEdge(string nodeKeyFrom, string nodeKeyTo, Stream value = null);

        void DeleteNode(string nodeKey);

        void DeleteEdge(string nodeKeyFrom, string nodeKeyTo);

        Stream GetNode(string nodeKey);
        Stream GetEdge(string nodeKeyFrom, string nodeKeyTo);

        void SaveChanges();

        IEnumerable<string> GetAdjacent(string nodeKey);

        bool IsIsolated(string nodeKey);
        
        Iterator<Edge> IterateEdges();
        Iterator<Node> IterateNodes();
    }
}
