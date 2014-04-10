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
        void PutEdge(string nodeKeyFrom, string nodeKeyTo);

        void DeleteNode(string nodeKey);

        void DeleteEdge(string nodeKeyFrom, string nodeKeyTo);

        Stream Get(string nodeKey);

        void SaveChanges();
    }
}
