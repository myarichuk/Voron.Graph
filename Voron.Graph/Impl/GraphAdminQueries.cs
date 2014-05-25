using Newtonsoft.Json.Linq;
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
                        CheckTransactionDisposedAndThrowIfNeeded(tx);

                        var readResult = nodesIterator.CreateReaderForCurrent();
                        using (var readResultAsStream = readResult.AsStream())
                        {
                            Etag etag;
                            JObject value;
                            Util.EtagAndValueFromStream(readResultAsStream, out etag, out value);

                            results.Add(new Node(nodesIterator.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag));
                        }

                    } while (nodesIterator.MoveNext());
                }

                CheckTransactionDisposedAndThrowIfNeeded(tx);
                return results;
            });
        }

        public Task<List<Edge>> GetAllEdges(Transaction tx, CancellationToken cancelToken)
        {
            return Task.Run(() => GetEdges(tx, null, ref cancelToken));
        }

        public Task<List<Edge>> GetEdgesOfNode(Transaction tx, Node node, CancellationToken cancelToken)
        {
            return Task.Run(() => GetEdges(tx, node.Key.ToSlice(), ref cancelToken));
        }

        private static void CheckTransactionDisposedAndThrowIfNeeded(Transaction tx)
        {
            if (tx.IsDisposed)
                throw new InvalidOperationException("Transaction was disposed before the operation has been complete.");
        }

        private static List<Edge> GetEdges(Transaction tx,Slice requiredPrefix, ref CancellationToken cancelToken)
        {
            var results = new List<Edge>();
            using (var edgesIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
            {
                if (requiredPrefix != null)
                {
                    edgesIterator.RequiredPrefix = requiredPrefix;
                    edgesIterator.Seek(requiredPrefix);
                }
                else if (!edgesIterator.Seek(Slice.BeforeAllKeys))
                    return results;

                do
                {
                    cancelToken.ThrowIfCancellationRequested();
                    CheckTransactionDisposedAndThrowIfNeeded(tx);

                    var readResult = edgesIterator.CreateReaderForCurrent();
                    using (var readResultAsStream = readResult.AsStream())
                    {
                        Etag etag;
                        JObject value;

                        Util.EtagAndValueFromStream(readResultAsStream, out etag, out value);
                        results.Add(new Edge(edgesIterator.CurrentKey.ToEdgeTreeKey(), value, etag));
                    }

                } while (edgesIterator.MoveNext());
            }

            return results;
        }
    }
}
