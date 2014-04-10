using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public interface ISearchVisitor
    {
        bool HasMatch(string keyNode, Stream value);
    }
}
