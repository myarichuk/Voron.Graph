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
    public class GraphCommands
    {
        private readonly string _nodesTreeName;
        private readonly string _edgesTreeName;
        private readonly string _disconnectedNodesTreeName;
        private readonly Conventions _conventions;
        private readonly GraphQueries _graphQueries;

        internal GraphCommands(GraphQueries graphQueries, string nodesTreeName, string edgesTreeName, string disconnectedNodesTreeName, Conventions conventions)
        {
            _graphQueries = graphQueries;
            _nodesTreeName = nodesTreeName;
            _edgesTreeName = edgesTreeName;
            _disconnectedNodesTreeName = disconnectedNodesTreeName;
            _conventions = conventions;
        }

        public Node CreateNode(Transaction tx, JObject value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var key = _conventions.GetNextNodeKey();

            var nodeKey = key.ToSlice();

            tx.NodeTree.Add(tx.VoronTransaction, nodeKey, value.ToStream());
            tx.DisconnectedNodeTree.Add(tx.VoronTransaction, nodeKey, value.ToStream());

            return new Node(key, value);
        }

        public Node CreateNode(Transaction tx)
        {
            return CreateNode(tx, new JObject());
        }

        public bool TryUpdate(Transaction tx, Node node)
        {
            if (!_graphQueries.ContainsNode(tx, node))
                return false;

            //Voron's method name here is misleading --> it performs updates as well
            tx.NodeTree.Add(tx.VoronTransaction, node.Key.ToSlice(), node.Data.ToStream());
            return true;
        }

        public Edge CreateEdgeBetween(Transaction tx, Node nodeFrom, Node nodeTo, JObject value = null, ushort type = 0)
        {
            if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (!_graphQueries.ContainsNode(tx, nodeFrom.Key))
                throw new ArgumentException("nodeFrom does not exist in the tree", "nodeFrom");

            if (nodeTo == null) throw new ArgumentNullException("nodeTo");
            if (!_graphQueries.ContainsNode(tx, nodeTo.Key))
                throw new ArgumentException("nodeTo does not exist in the tree", "nodeTo");

            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value);
            tx.EdgeTree.Add(tx.VoronTransaction, edge.Key.ToSlice(), value.ToStream() ?? Stream.Null);

            tx.DisconnectedNodeTree.Delete(tx.VoronTransaction, nodeFrom.Key.ToSlice());

            return edge;
        }

        public void Delete(Transaction tx, Node node)
        {
            var nodeKey = node.Key.ToSlice();
            tx.NodeTree.Delete(tx.VoronTransaction, nodeKey);
            foreach (var edge in GetEdgesOf(tx, node))
                tx.EdgeTree.Delete(tx.VoronTransaction, edge.Key.ToSlice());
        }

        public void Delete(Transaction tx, Edge edge)
        {
            tx.EdgeTree.Delete(tx.VoronTransaction, edge.Key.ToSlice());
        }

        public IEnumerable<Edge> GetEdgesOf(Transaction tx, Node node)
        {
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

                    using (var edgeValueAsStream = edgeValueReader.AsStream())
                    {
                        var edge = new Edge(edgeKey, edgeValueAsStream.ToJObject());

                        yield return edge;
                    }
                } while (edgeIterator.MoveNext());
            }

        }

    }
}
