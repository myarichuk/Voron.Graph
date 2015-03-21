using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using Voron.Graph.Extensions;

namespace Voron.Graph.Impl
{
    public class GraphCommands
    {
        private readonly Conventions _conventions;
        private readonly GraphQueries _graphQueries;

        internal GraphCommands(GraphQueries graphQueries, Conventions conventions)
        {
            _graphQueries = graphQueries;
            _conventions = conventions;
        }      

        public void PutToSystemMetadata<T>(Transaction tx, string key, T value)
        {
            var metadataReadResult = tx.SystemTree.Read(tx.GraphMetadataKey);
            Debug.Assert(metadataReadResult != null && metadataReadResult.Version > 0);

            using (var metadataStream = metadataReadResult.Reader.AsStream())
            {
                Debug.Assert(metadataStream != null);

                var metadata = metadataStream.ToJObject();
                metadata[key] = JToken.FromObject(value);

                tx.SystemTree.Add(tx.GraphMetadataKey, metadata.ToStream());
            }            
        }

        public Node CreateNode(Transaction tx, JObject value)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (value == null) throw new ArgumentNullException("value");

            var key = _conventions.GetNextNodeKey();

            var nodeKey = key.ToSlice();
            var etag = Etag.Generate();

            tx.NodeTree.Add(nodeKey, Util.EtagAndValueToStream(etag,value));
            tx.KeyByEtagTree.Add(etag.ToSlice(), nodeKey);
            tx.DisconnectedNodeTree.Add(nodeKey, value.ToStream());

			var node = new Node(key, value, etag);

	        return node;
        }


        public Node CreateNode(Transaction tx)
        {
            return CreateNode(tx, new JObject());
        }

        public bool TryUpdate(Transaction tx, Node node)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        if (!_graphQueries.ContainsNode(tx, node))
                return false;

            //Voron's method name here is misleading --> it performs updates as well
            tx.KeyByEtagTree.Delete(node.Etag.ToSlice());
            var newEtag = Etag.Generate();
            node.Etag = newEtag;

            tx.NodeTree.Add(node.Key.ToSlice(), Util.EtagAndValueToStream(newEtag, node.Data));

            tx.KeyByEtagTree.Add(newEtag.ToSlice(), node.Key.ToSlice());
            return true;
        }

        public Edge CreateEdgeBetween(Transaction tx, Node nodeFrom, Node nodeTo, JObject value = null, ushort type = 0,short edgeWeight = 1)
        {
	        if (tx == null) throw new ArgumentNullException("tx");

	        if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (!_graphQueries.ContainsNode(tx, nodeFrom.Key))
                throw new ArgumentException("nodeFrom does not exist in the tree", "nodeFrom");

            if (nodeTo == null) throw new ArgumentNullException("nodeTo");
            if (!_graphQueries.ContainsNode(tx, nodeTo.Key))
                throw new ArgumentException("nodeTo does not exist in the tree", "nodeTo");
            
            var newEtag = Etag.Generate();
            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value,type,newEtag,edgeWeight);
            
            tx.DisconnectedNodeTree.Delete(nodeFrom.Key.ToSlice());
            tx.KeyByEtagTree.Add(edge.Etag.ToSlice(), edge.Key.ToSlice());

            tx.EdgeTree.Add(edge.Key.ToSlice(), Util.EtagWeightAndValueToStream(newEtag,value ?? new JObject(), edgeWeight));

            return edge;
        }

        public void Delete(Transaction tx, Node node)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        var nodeKey = node.Key.ToSlice();
            tx.NodeTree.Delete(nodeKey);
            foreach (var edge in _graphQueries.GetEdgesOf(tx, node))
                tx.EdgeTree.Delete(edge.Key.ToSlice());

            tx.KeyByEtagTree.Delete(node.Etag.ToSlice());
        }

        public void Delete(Transaction tx, Edge edge)
        {
            tx.EdgeTree.Delete(edge.Key.ToSlice());
            tx.KeyByEtagTree.Delete(edge.Etag.ToSlice());
        }

       
    }
}
