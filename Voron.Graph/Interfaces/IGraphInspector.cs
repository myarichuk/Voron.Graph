namespace Voron.Graph
{
    public interface IGraphInspector
    {
        void AcceptVisitor(IVisitor visitor);
    }
}
