using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public interface IGraphDetective
    {
        Stream FindOne(Func<string, Stream, bool> predicate);
        
        bool Contains(Func<string, Stream, bool> predicate);
    }
}
