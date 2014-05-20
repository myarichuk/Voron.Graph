using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Trees;

namespace Voron.Graph
{
    public class GraphAdminQueries
    {
        private readonly string _nodesTreeName;
        private readonly string _edgesTreeName;
        private readonly string _disconnectedNodesTreeName;

        internal GraphAdminQueries(string nodesTreeName, string edgesTreeName, string disconnectedNodesTreeName)
        {
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _nodesTreeName = nodesTreeName;
            _edgesTreeName = edgesTreeName;
        }

        public Task<List<Node>> GetAllNodes(Transaction tx, CancellationToken cancelToken)
        {
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

    }
}
