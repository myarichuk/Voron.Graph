using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Voron.Graph.Impl
{
    public class GraphQueries
    {
        public IEnumerable<Node> GetAdjacentOf(Transaction tx, Node node, ushort type = 0)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        var alreadyRetrievedKeys = new HashSet<long>();
            using (var edgeIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
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
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        using (var edgeIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
            {
                edgeIterator.RequiredPrefix = node.Key.ToSlice();
                return edgeIterator.Seek(Slice.BeforeAllKeys);
            }
        }

	    public bool ContainsEdge(Transaction tx, Edge edge)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (edge == null) throw new ArgumentNullException("edge");

	        return tx.EdgeTree.ReadVersion(tx.VoronTransaction, edge.Key.ToSlice()) > 0;
        }

	    public bool ContainsNode(Transaction tx, Node node)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        return ContainsNode(tx, node.Key);
        }

	    public bool ContainsNode(Transaction tx, long nodeKey)
        {
	        if (tx == null) throw new ArgumentNullException("tx");

	        return tx.NodeTree.ReadVersion(tx.VoronTransaction, nodeKey.ToSlice()) > 0;
        }

	    public Node LoadNode(Transaction tx, long nodeKey)
        {
	        if (tx == null) throw new ArgumentNullException("tx");

	        var readResult = tx.NodeTree.Read(tx.VoronTransaction, nodeKey.ToSlice());
            if (readResult == null)
                return null;

            using (var valueStream = readResult.Reader.AsStream())
                return new Node(nodeKey, valueStream.ToJObject());
        }


        public IEnumerable<Edge> GetEdgesBetween(Transaction tx, Node nodeFrom, Node nodeTo, ushort? type = null)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (nodeFrom == null)
                throw new ArgumentNullException("nodeFrom");
            if (nodeTo == null)
                throw new ArgumentNullException("nodeTo");

            using (var edgeIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
            {
                edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);
                if (!edgeIterator.Seek(edgeIterator.RequiredPrefix))
                    yield break;

                do
                {
                    var edgeTreeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (type.HasValue && edgeTreeKey.Type != type)
                        continue;

                    var valueReader = edgeIterator.CreateReaderForCurrent();
                    using (var valueStream = valueReader.AsStream() ?? Stream.Null)
	                    yield return new Edge(edgeTreeKey, valueStream.ToJObject());

                } while (edgeIterator.MoveNext());
            }
        }
    }
}
