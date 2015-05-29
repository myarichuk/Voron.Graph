using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Voron.Graph.Extensions;
using Voron.Graph.Primitives;

namespace Voron.Graph
{
	public partial class GraphStorage
	{	   
		public IEnumerable<Edge> GetEdgesOf(Transaction tx, Node node)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (node == null) throw new ArgumentNullException("node");

			using (var edgeIterator = tx.EdgeTree.Iterate())
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
                        var edgeValueReader = edgeIterator.CreateReaderForCurrent();

                        using (var edgeEtagAndValueAsStream = edgeValueReader.AsStream())
                        {
                            Etag etag;
                            JObject value;
                            short weight;

                            Util.EtagWeightAndValueFromStream(edgeEtagAndValueAsStream, out etag, out weight, out value);
                            var edge = new Edge(edgeKey.NodeKeyFrom, edgeKey.NodeKeyTo, value, edgeKey.Type, etag, weight);

                            var nodeWithEdge = new NodeWithEdge
                            {
                                Node = adjacentNode,
                                EdgeTo = edge
                            };
							yield return nodeWithEdge;
                        }
                    }
				} while (edgeIterator.MoveNext());
			}
		}

      

		public bool IsIsolated(Transaction tx, Node node)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (node == null) throw new ArgumentNullException("node");

			using (var edgeIterator = tx.EdgeTree.Iterate())
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

			var readResult = tx.NodeTree.Read(nodeKey.ToSlice());
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

		public IEnumerable<Node> LoadMultipleNodes(Transaction tx, IEnumerable<long> nodeKeys)
		{
			return nodeKeys.Select(key => LoadNode(tx, key));
		}

		public IEnumerable<Edge> GetEdgesBetween(Transaction tx, Node nodeFrom, Node nodeTo, ushort? type = null)
		{
			if (tx == null) throw new ArgumentNullException("tx");
			if (nodeFrom == null)
				throw new ArgumentNullException("nodeFrom");
			if (nodeTo == null)
				throw new ArgumentNullException("nodeTo");

			using (var edgeIterator = tx.EdgeTree.Iterate())
			{
				edgeIterator.RequiredPrefix = Util.EdgeKeyPrefix(nodeFrom, nodeTo);
				if (!edgeIterator.Seek(edgeIterator.RequiredPrefix))
					yield break;

				do
				{
					var edgeKey = edgeIterator.CurrentKey.ToEdgeTreeKey();
					if (type.HasValue && edgeKey.Type != type)
						continue;

					var valueReader = edgeIterator.CreateReaderForCurrent();
					using (var etagAndValueAsStream = valueReader.AsStream())
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