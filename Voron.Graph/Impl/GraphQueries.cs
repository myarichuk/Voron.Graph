using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Voron.Graph.Extensions;

namespace Voron.Graph.Impl
{
    public class GraphQueries
    {
        public T GetFromSystemMetadata<T>(Transaction tx, string key)
        {
            var metadataReadResult = tx.SystemTree.Read(tx.VoronTransaction, tx.GraphMetadataKey);
            Debug.Assert(metadataReadResult.Version > 0);

            using (var metadataStream = metadataReadResult.Reader.AsStream())
            {
                if (metadataStream == null)
                    return default(T);

                var metadata = metadataStream.ToJObject();
                return metadata.Value<T>(key);
            }
        }

        public IEnumerable<Edge> GetEdgesOf(Transaction tx, Node node)
        {
            if (tx == null) throw new ArgumentNullException("tx");
            if (node == null) throw new ArgumentNullException("node");

            using (var edgeIterator = tx.EdgeTree.Iterate(tx.VoronTransaction))
            {
                var nodeKey = node.Key.ToSlice();
                edgeIterator.RequiredPrefix = nodeKey;
                if (!edgeIterator.Seek(nodeKey))
                    yield break;

                do
                {
                    var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    var edgeValueReader = edgeIterator.CreateReaderForCurrent();

                    using (var edgeEtagAndValueAsStream = edgeValueReader.AsStream())
                    {
                        Etag etag;
                        JObject value;

                        Util.EtagAndValueFromStream(edgeEtagAndValueAsStream, out etag, out value);
                        var edge = new Edge(edgeKey,value,etag);
                        
                        yield return edge;
                    }
                } while (edgeIterator.MoveNext());
            }
        }

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

            using (var etagAndValueAsStream = readResult.Reader.AsStream())
            {
                Etag etag;
                JObject value;

                Util.EtagAndValueFromStream(etagAndValueAsStream, out etag, out value); 
                return new Node(nodeKey, value, etag);
            }
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
                    using (var etagAndValueAsStream = valueReader.AsStream())
                    {
                        Etag etag;
                        JObject value;

                        Util.EtagAndValueFromStream(etagAndValueAsStream, out etag, out value);
                        yield return new Edge(edgeTreeKey, value, etag);
                    }

                } while (edgeIterator.MoveNext());
            }
        }
    }
}
