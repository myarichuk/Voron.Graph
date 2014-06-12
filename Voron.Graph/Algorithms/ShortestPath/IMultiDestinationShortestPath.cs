using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public interface IMultiDestinationShortestPath
    {
        IMultiDestinationShortestPathResults Execute();
        Task<IMultiDestinationShortestPathResults> ExecuteAsync();
    }
}
