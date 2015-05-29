using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using Voron.Graph.Extensions;

namespace Voron.Graph
{
    public partial class GraphStorage
    {
		public class GraphAdmin
		{
			internal GraphAdmin() { }

			public IEnumerable<Node> GetAllNodes(Transaction tx, CancellationToken cancelToken)
			{
				if (tx == null) throw new ArgumentNullException("tx");
				using (var nodesIterator = tx.NodeTree.Iterate())
				{
					if (!nodesIterator.Seek(Slice.BeforeAllKeys))
						yield break;

					do
					{
						cancelToken.ThrowIfCancellationRequested();
						if (tx.IsDisposed)
							throw new InvalidOperationException("Transaction was disposed before the operation has been complete.");

						var readResult = nodesIterator.CreateReaderForCurrent();
						using (var readResultAsStream = readResult.AsStream())
						{
							Etag etag;
							JObject value;
							Util.EtagAndValueFromStream(readResultAsStream, out etag, out value);

							yield return new Node(nodesIterator.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag);
						}

					} while (nodesIterator.MoveNext());
				}
			}

			public IEnumerable<Edge> GetAllEdges(Transaction tx)
			{
				return GetEdges(tx, null);
			}

			public IEnumerable<Edge> GetEdgesOfNode(Transaction tx, Node node)
			{
				return GetEdges(tx, node.Key.ToSlice());
			}

			private static IEnumerable<Edge> GetEdges(Transaction tx, Slice requiredPrefix)
			{
				using (var edgesIterator = tx.EdgeTree.Iterate())
				{
					if (requiredPrefix != null)
					{
						edgesIterator.RequiredPrefix = requiredPrefix;
						edgesIterator.Seek(requiredPrefix);
					}
					else if (!edgesIterator.Seek(Slice.BeforeAllKeys))
						yield break;

					do
					{
						if (tx.IsDisposed)
							throw new InvalidOperationException("Transaction was disposed before the operation has been complete.");

						var readResult = edgesIterator.CreateReaderForCurrent();
						using (var readResultAsStream = readResult.AsStream())
						{
							Etag etag;
							JObject value;
							var edgeTreeKey = edgesIterator.CurrentKey.ToEdgeTreeKey();
							short weight;
							Util.EtagWeightAndValueFromStream(readResultAsStream, out etag, out weight, out value);
							yield return new Edge(edgeTreeKey.NodeKeyFrom, edgeTreeKey.NodeKeyTo, value, edgeTreeKey.Type, etag, weight);
						}

					} while (edgesIterator.MoveNext());
				}

			}
		}
    }
}
