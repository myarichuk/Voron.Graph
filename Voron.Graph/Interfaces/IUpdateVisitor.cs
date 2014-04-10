using System;
using System.IO;

namespace Voron.Graph
{
    public interface IUpdateVisitor
    {
        bool ShouldUpdate(string nodeKey);

        void Update(Func<string, Stream, Stream> updateFunc);
    }
}
