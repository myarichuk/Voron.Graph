using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public interface ISingleSourceShortestPath
    {
        ISingleSourceShortestPathResults Execute();
        Task<ISingleSourceShortestPathResults> ExecuteAsync();
    }
}
