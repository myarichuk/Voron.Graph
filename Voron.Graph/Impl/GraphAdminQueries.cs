using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Voron.Graph.Impl
{
    public class GraphAdminQueries
    {
        public Task<List<Node>> GetAllNodes(Transaction tx, CancellationToken cancelToken)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        return Task.Run(() =>
            {
                var results = new List<Node>();
                using (var nodesIterator = tx.NodeTree.Iterate(tx.VoronTransaction))
                {
                    if (!nodesIterator.Seek(Slice.BeforeAllKeys))
                        return results;

                    do
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        var readResult = nodesIterator.CreateReaderForCurrent();
                        using (var readResultAsStream = readResult.AsStream())
                            results.Add(new Node(nodesIterator.CurrentKey.CreateReader().ReadBigEndianInt64(), readResultAsStream.ToJObject()));

                    } while (nodesIterator.MoveNext());
                }

                return results;
            });
        }

        public Task<List<Edge>> GetAllEdges(Transaction tx, CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {
                var results = new List<Edge>();
                using (var edgesIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
                {
                    if (!edgesIterator.Seek(Slice.BeforeAllKeys))
                        return results;

                    do
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        var readResult = edgesIterator.CreateReaderForCurrent();
                        using (var readResultAsStream = readResult.AsStream())
                            results.Add(new Edge(edgesIterator.CurrentKey.ToEdgeTreeKey(), readResultAsStream.ToJObject()));

                    } while (edgesIterator.MoveNext());
                }

                return results;
            });
        }
    }
}
