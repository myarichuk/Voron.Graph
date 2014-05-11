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

        Stream Get(string nodeKey);

        void SaveChanges();

        IEnumerable<string> GetAdjacent(string nodeKey);

        bool IsIsolated(string nodeKey);
    }
}
