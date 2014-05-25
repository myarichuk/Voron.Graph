using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var metadataReadResult = tx.SystemTree.Read(tx.VoronTransaction, tx.GraphMetadataKey);
            Debug.Assert(metadataReadResult.Version > 0);

            using (var metadataStream = metadataReadResult.Reader.AsStream())
            {
                Debug.Assert(metadataStream != null);

                var metadata = metadataStream.ToJObject();
                metadata[key] = JToken.FromObject(value);

                tx.SystemTree.Add(tx.VoronTransaction, tx.GraphMetadataKey, metadata.ToStream());
            }            
        }

        public Node CreateNode(Transaction tx, JObject value)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (value == null) throw new ArgumentNullException("value");

            var key = _conventions.GetNextNodeKey();

            var nodeKey = key.ToSlice();
            var etag = Etag.Generate();

            tx.NodeTree.Add(tx.VoronTransaction, nodeKey, Util.EtagAndValueToStream(etag,value));
            tx.KeyByEtagTree.Add(tx.VoronTransaction, etag.ToSlice(), nodeKey);
            tx.DisconnectedNodeTree.Add(tx.VoronTransaction, nodeKey, value.ToStream());

            return new Node(key, value, etag);
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
            tx.KeyByEtagTree.Delete(tx.VoronTransaction, node.Etag.ToSlice());
            var newEtag = Etag.Generate();
            node.Etag = newEtag;

            tx.NodeTree.Add(tx.VoronTransaction, node.Key.ToSlice(), Util.EtagAndValueToStream(newEtag, node.Data));

            tx.KeyByEtagTree.Add(tx.VoronTransaction, newEtag.ToSlice(), node.Key.ToSlice());
            return true;
        }

        public Edge CreateEdgeBetween(Transaction tx, Node nodeFrom, Node nodeTo, JObject value = null, ushort type = 0)
        {
	        if (tx == null) throw new ArgumentNullException("tx");

	        if (nodeFrom == null) throw new ArgumentNullException("nodeFrom");
            if (!_graphQueries.ContainsNode(tx, nodeFrom.Key))
                throw new ArgumentException("nodeFrom does not exist in the tree", "nodeFrom");

            if (nodeTo == null) throw new ArgumentNullException("nodeTo");
            if (!_graphQueries.ContainsNode(tx, nodeTo.Key))
                throw new ArgumentException("nodeTo does not exist in the tree", "nodeTo");
            
            var newEtag = Etag.Generate();
            var edge = new Edge(nodeFrom.Key, nodeTo.Key, value, type, newEtag);
            
            tx.DisconnectedNodeTree.Delete(tx.VoronTransaction, nodeFrom.Key.ToSlice());
            tx.KeyByEtagTree.Add(tx.VoronTransaction, edge.Etag.ToSlice(), edge.Key.ToSlice());

            tx.EdgeTree.Add(tx.VoronTransaction, edge.Key.ToSlice(), Util.EtagAndValueToStream(newEtag,value ?? new JObject()));

            return edge;
        }

        public void Delete(Transaction tx, Node node)
        {
	        if (tx == null) throw new ArgumentNullException("tx");
	        if (node == null) throw new ArgumentNullException("node");

	        var nodeKey = node.Key.ToSlice();
            tx.NodeTree.Delete(tx.VoronTransaction, nodeKey);
            foreach (var edge in _graphQueries.GetEdgesOf(tx, node))
                tx.EdgeTree.Delete(tx.VoronTransaction, edge.Key.ToSlice());

            tx.KeyByEtagTree.Delete(tx.VoronTransaction, node.Etag.ToSlice());
        }

        public void Delete(Transaction tx, Edge edge)
        {
            tx.EdgeTree.Delete(tx.VoronTransaction, edge.Key.ToSlice());
            tx.KeyByEtagTree.Delete(tx.VoronTransaction, edge.Etag.ToSlice());
        }

       
    }
}
