using System.IO;

namespace Voron.Graph
{
    public interface IVisitor
    {
        void Visit(string nodeKey, Stream value);
    }
}
