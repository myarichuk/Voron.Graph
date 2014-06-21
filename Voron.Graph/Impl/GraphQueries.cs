using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using Voron.Graph.Extensions;
using Voron.Trees;
using Voron.Graph.Primitives;

namespace Voron.Graph.Impl
{
	public class GraphQueries
	{		

		public T GetFromSystemMetadata<T>(Transaction tx, string key)
		{
			ReadResult metadataReadResult = tx.SystemTree.Read(tx.GraphMetadataKey);
			Debug.Assert(metadataReadResult.Version > 0);

			using (Stream metadataStream = metadataReadResult.Reader.AsStream())
			{
				if (metadataStream == null)
					return default(T);

				JObject metadata = metadataStream.ToJObject();
				return metadata.Value<T>(key);
			}
		}

		public IEnumerable<Edge> GetEdgesOf(Transaction tx, Node node)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (node == null) throw new ArgumentNullException("node");

			using (TreeIterator edgeIterator = tx.EdgeTree.Iterate())
			{
				Slice nodeKey = node.Key.ToSlice();
				edgeIterator.RequiredPrefix = nodeKey;
				if (!edgeIterator.Seek(nodeKey))
					yield break;

				do
				{
					EdgeTreeKey edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
					ValueReader edgeValueReader = edgeIterator.CreateReaderForCurrent();

					using (Stream edgeEtagAndValueAsStream = edgeValueReader.AsStream())
					{
						Etag etag;
						JObject value;
                        short weight;
						Util.EtagWeightAndValueFromStream(edgeEtagAndValueAsStream, out etag, out weight, out value);
                        var edge = new Edge(edgeKey.NodeKeyFrom, edgeKey.NodeKeyTo, value, edgeKey.Type, etag, weight);

						yield return edge;
					}
				} while (edgeIterator.MoveNext());
			}
		}

        public bool IsSink(Transaction tx, Node node)
        {
            using (var edgeIterator = tx.EdgeTree.Iterate())
            {
                var nodeKey = node.Key.ToSlice();
                edgeIterator.RequiredPrefix = nodeKey;
                return !edgeIterator.Seek(nodeKey);
            }
        }

        public IEnumerable<NodeWithEdge> GetAdjacentOf(Transaction tx, Node node, Func<ushort,bool> typePredicate = null)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (node == null) throw new ArgumentNullException("node");

			var alreadyRetrievedKeys = new HashSet<long>();
			using (var edgeIterator = tx.EdgeTree.Iterate())
			{
				var nodeKey = node.Key.ToSlice();
				edgeIterator.RequiredPrefix = nodeKey;
				if (!edgeIterator.Seek(nodeKey))
					yield break;

				do
				{
					var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
                    if (typePredicate != null && typePredicate(edgeKey.Type))
						continue;

                    if (!alreadyRetrievedKeys.Contains(edgeKey.NodeKeyTo))
                    {
                        alreadyRetrievedKeys.Add(edgeKey.NodeKeyTo);
                        var adjacentNode = LoadNode(tx, edgeKey.NodeKeyTo);
                        ValueReader edgeValueReader = edgeIterator.CreateReaderForCurrent();

                        using (Stream edgeEtagAndValueAsStream = edgeValueReader.AsStream())
                        {
                            Etag etag;
                            JObject value;
                            short weight;

                            Util.EtagWeightAndValueFromStream(edgeEtagAndValueAsStream, out etag, out weight, out value);
                            var edge = new Edge(edgeKey.NodeKeyFrom, edgeKey.NodeKeyTo, value, edgeKey.Type, etag, weight);

                            yield return new NodeWithEdge
                            {
                                Node = adjacentNode,
                                EdgeTo = edge
                            };
                        }
                    }
				} while (edgeIterator.MoveNext());
			}
		}

      

		public bool IsIsolated(Transaction tx, Node node)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (node == null) throw new ArgumentNullException("node");

			using (TreeIterator edgeIterator = tx.EdgeTree.Iterate())
			{
				edgeIterator.RequiredPrefix = node.Key.ToSlice();
				return edgeIterator.Seek(Slice.BeforeAllKeys);
			}
		}

		public bool ContainsEdge(Transaction tx, Edge edge)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (edge == null) throw new ArgumentNullException("edge");

			return tx.EdgeTree.ReadVersion(edge.Key.ToSlice()) > 0;
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

			return tx.NodeTree.ReadVersion(nodeKey.ToSlice()) > 0;
		}

		public Node LoadNode(Transaction tx, long nodeKey)
		{
			if (tx == null) throw new ArgumentNullException("tx");

			ReadResult readResult = tx.NodeTree.Read(nodeKey.ToSlice());
			if (readResult == null)
				return null;

			using (Stream etagAndValueAsStream = readResult.Reader.AsStream())
			{
				Etag etag;
				JObject value;

				Util.EtagAndValueFromStream(etagAndValueAsStream, out etag, out value);
				return new Node(nodeKey, value, etag);
			}
		}

		public IEnumerable<Node> LoadMultipleNodes(Transaction tx, IEnumerable<long> nodeKeys)
        {
            foreach (var key in nodeKeys)
                yield return LoadNode(tx, key);
        }

		public IEnumerable<Edge> GetEdgesBetween(Transaction tx, Node nodeFrom, Node nodeTo, ushort? type = null)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (nodeFrom == null)
				throw new ArgumentNullException("nodeFrom");
			if (nodeTo == null)
				throw new ArgumentNullException("nodeTo");

			using (TreeIterator edgeIterator = tx.EdgeTree.Iterate())
			{
				edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);
				if (!edgeIterator.Seek(edgeIterator.RequiredPrefix))
					yield break;

				do
				{
					var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
					if (type.HasValue && edgeKey.Type != type)
						continue;

					ValueReader valueReader = edgeIterator.CreateReaderForCurrent();
					using (Stream etagAndValueAsStream = valueReader.AsStream())
					{
						Etag etag;
						JObject value;
                        short weight;
						Util.EtagWeightAndValueFromStream(etagAndValueAsStream, out etag, out weight, out value);
                        var edge = new Edge(edgeKey.NodeKeyFrom, edgeKey.NodeKeyTo, value, edgeKey.Type, etag, weight);
                        yield return edge;
                    }
				} while (edgeIterator.MoveNext());
			}
		}
	}
}