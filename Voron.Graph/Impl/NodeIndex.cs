using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Voron.Graph.Extensions;

namespace Voron.Graph.Impl
{
    public class NodeIndex
    {
        private readonly StorageEnvironment _storage;
        private readonly string _indexTableTreeName;
        private readonly IEnumerable<string> _indexedProperties;
        private const string IndexTableSuffix = "_IndexTable";

        internal NodeIndex(GraphStorage graph)
        {
            if (graph == null) throw new ArgumentNullException("graph");
            _storage = graph.StorageEnvironment;
            _indexedProperties = graph.IndexedProperties;
            _indexTableTreeName = graph.GraphName + IndexTableSuffix;
            CreateSchema();
        }

        public void IndexDataIfRelevant(Node node)
        {
            var fieldsToIndex = GetFieldsToIndex(node.Data).ToList();
            if (fieldsToIndex.Count > 0)
            {                
                using (var tx = _storage.NewTransaction(TransactionFlags.ReadWrite))
                {
                    var indexTree = tx.ReadTree(_indexTableTreeName);
                    foreach (var field in fieldsToIndex)
                    {
                        indexTree.MultiAdd(field.ToString(), node.Key.ToSlice());
                    }
                    tx.Commit();
                }
            }
        }

        //TODO : refactor to non-recursion here. (large data!)
        private IEnumerable<JToken> GetFieldsToIndex(JObject data)
        {
            var fieldsToIndex = new HashSet<JToken>();
            foreach (var entry in data)
            {
                if (_indexedProperties.Contains(entry.Key))
                    fieldsToIndex.Add(entry.Value);
                var innerObject = entry.Value as JObject;
                if (innerObject != null)
                    fieldsToIndex.UnionWith(GetFieldsToIndex(innerObject));
            }

            return fieldsToIndex;
        }

        private void CreateSchema()
        {
            using (var tx = _storage.NewTransaction(TransactionFlags.ReadWrite))
            {
                _storage.CreateTree(tx, _indexTableTreeName);
                tx.Commit();
            }
        }
    }
}
