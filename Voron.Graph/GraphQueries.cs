using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Trees;
namespace Voron.Graph
{
    public class GraphQueries
    {
        private readonly Tree _nodesTree;
        private readonly Tree _edgesTree;
        private readonly Tree _disconnectedNodesTree;

        internal GraphQueries(Tree nodesTree, Tree edgesTree, Tree disconnectedNodesTree)
        {
            _nodesTree = nodesTree;
            _edgesTree = edgesTree;
            _disconnectedNodesTree = disconnectedNodesTree;
        }

        public IEnumerable<Node> GetAdjacentOf(Transaction tx, Node node, ushort type = 0)
        {            
            var alreadyRetrievedKeys = new HashSet<long>();
            using (var edgeIterator = _edgesTree.Iterate(tx.VoronTransaction))
            {
                var nodeKey = node.Key.ToSlice();
                edgeIterator.RequiredPrefix = nodeKey;
                if (!edgeIterator.Seek(nodeKey))
                    yield break;

                do
                {
                    var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (edgeKey.Type != type)
                        continue;

                    if (!alreadyRetrievedKeys.Contains(edgeKey.NodeKeyTo))
                    {
                        alreadyRetrievedKeys.Add(edgeKey.NodeKeyTo);
                        var adjacentNode = LoadNode(tx, edgeKey.NodeKeyTo);
                        yield return adjacentNode;
                    }

                } while (edgeIterator.MoveNext());
            }
        }

        public bool IsIsolated(Transaction tx, Node node)
        {
            using (var edgeIterator = _edgesTree.Iterate(tx.VoronTransaction))
            {
                edgeIterator.RequiredPrefix = node.Key.ToSlice();
                return edgeIterator.Seek(Slice.BeforeAllKeys);
            }
        }

        public bool ContainsEdge(Transaction tx, Edge edge)
        {
            return _nodesTree.ReadVersion(tx.VoronTransaction, edge.Key.ToSlice()) > 0;
        }
        
        public bool ContainsNode(Transaction tx, Node node)
        {
            return ContainsNode(tx, node.Key);
        }
        
        public bool ContainsNode(Transaction tx, long nodeKey)
        {
            return _nodesTree.ReadVersion(tx.VoronTransaction, nodeKey.ToSlice()) > 0;
        }

        public Node LoadNode(Transaction tx, long nodeKey)
        {
            var readResult = _nodesTree.Read(tx.VoronTransaction, nodeKey.ToSlice());
            if (readResult == null)
                return null;

            using (var valueStream = readResult.Reader.AsStream())
                return new Node(nodeKey, valueStream.ToJObject());
        }


        public IEnumerable<Edge> GetEdgesBetween(Transaction tx, Node nodeFrom, Node nodeTo, Func<ushort, bool> typePredicate = null)
        {
            if (nodeFrom == null)
                throw new ArgumentNullException("nodeFrom");
            if (nodeTo == null)
                throw new ArgumentNullException("nodeTo");

            using (var edgeIterator = _edgesTree.Iterate(tx.VoronTransaction))
            {
                edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);
                if (!edgeIterator.Seek(edgeIterator.RequiredPrefix))
                    yield break;

                do
                {
                    var edgeTreeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (typePredicate != null && !typePredicate(edgeTreeKey.Type))
                        continue;

                    var valueReader = edgeIterator.CreateReaderForCurrent();
                    using (var valueStream = valueReader.AsStream() ?? Stream.Null)
                    {
                        var jsonValue = valueStream.Length > 0 ? valueStream.ToJObject() : new JObject();
                        yield return new Edge(edgeTreeKey, valueStream.ToJObject());
                    }

                } while (edgeIterator.MoveNext());
            }
        }
    }
}
