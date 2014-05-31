using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public interface IShortestPathAlgorithm
    {
        IShortestPathResults Execute();
        Task<IShortestPathResults> ExecuteAsync();
    }
}
