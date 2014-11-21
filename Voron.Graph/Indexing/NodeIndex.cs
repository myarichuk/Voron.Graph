using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using Voron.Graph.Extensions;
using Voron.Impl;
using Voron.Trees;

namespace Voron.Graph.Indexing
{
    public class NodeIndex
    {
        private readonly StorageEnvironment _storage;
		private readonly string _indexPropertiesTreeName;
		private readonly string _indexTermsTreeName;
		private readonly HashSet<string> _indexedProperties;
	    private readonly ITermsParser _termsParser;

		private const string IndexedPropertiesTableSuffix = "_IndexPropertiesTable";
		private const string IndexedTermsTableSuffix = "_IndexTermsTable";

        internal NodeIndex(GraphStorage graph, ITermsParser termsParser)
        {
	        if (graph == null) throw new ArgumentNullException("graph");
	        if (termsParser == null) throw new ArgumentNullException("termsParser");
			
			_termsParser = termsParser;
			_storage = graph.StorageEnvironment;
            _indexedProperties = new HashSet<string>(graph.IndexedProperties);
			_indexPropertiesTreeName = graph.GraphName + IndexedPropertiesTableSuffix;
			_indexTermsTreeName = graph.GraphName + IndexedTermsTableSuffix;
			CreateSchema();
        }

	    public void IndexIfRelevant(Node node)
	    {
		    var writeBatch = new WriteBatch();
			var fieldsToIndex = FieldsToIndex(node.Data);
			
			foreach (var field in fieldsToIndex)
				IndexField(node.Key, field.Key, field.Value, writeBatch);

			_storage.Writer.Write(writeBatch);
	    }

	    public Dictionary<string,JToken> FieldsToIndex(JObject data)
	    {
		    var dataProperties = data.Properties();

		    var fieldsToIndex = 
				(from property in dataProperties 
				where _indexedProperties.Contains(property.Name, StringComparer.InvariantCultureIgnoreCase) 
				select property).ToDictionary(p => p.Name,p => p.Value);

		    return fieldsToIndex;
	    }

	    public HashSet<long> FulltextQuery(string value)
	    {
		    var nodeKeys = new HashSet<long>();
		    using (var snapshot = _storage.CreateSnapshot())
		    {
			    var valueTerms = _termsParser.GetTerms(value);

			    foreach (var term in valueTerms)
			    {
				    if (term.Length >= _termsParser.MinimumTermSize)
				    {
					    using (var iter = snapshot.MultiRead(_indexTermsTreeName, term))
						    IterateAndGetNodeKeys(iter, nodeKeys);
				    }
				    else
				    {
					    using (var termsIter = snapshot.Iterate(_indexTermsTreeName))
					    {
						    termsIter.RequiredPrefix = term;
							if (termsIter.Seek(term))
							    do
							    {
								    using (var iter = snapshot.MultiRead(_indexTermsTreeName, termsIter.CurrentKey))
										IterateAndGetNodeKeys(iter, nodeKeys);
								} while (termsIter.MoveNext());
					    }
				    }
			    }
		    }

		    return nodeKeys;
	    }

	    private static void IterateAndGetNodeKeys(IIterator iter, HashSet<long> nodeKeys)
	    {
		    if (iter.Seek(Slice.BeforeAllKeys))
		    {
			    do
			    {
				    var currentKey = iter.CurrentKey.ToNodeKey();
				    nodeKeys.Add(currentKey);
			    } while (iter.MoveNext());
		    }
	    }

	    private void IndexField(
			long nodeKey, 
			string fieldKey, 
			JToken valueToken,
			WriteBatch writeBatch)
		{
			//save field <-> node reference - if needed we can check whether certain field name exists in a specific node
			writeBatch.MultiAdd(fieldKey, nodeKey.ToSlice(), _indexPropertiesTreeName);

			var propertyValueToken = valueToken as JValue;
			if (propertyValueToken == null || valueToken.HasValues)
				throw new InvalidOperationException("Only primitive type properties can be indexed.");

			var terms = _termsParser.GetTerms(propertyValueToken).ToList();
			terms.ForEach(term => writeBatch.MultiAdd(term.ToLower(), nodeKey.ToSlice(), _indexTermsTreeName));
		}
		
		private void CreateSchema()
        {
            using (var tx = _storage.NewTransaction(TransactionFlags.ReadWrite))
            {
				_storage.CreateTree(tx, _indexPropertiesTreeName);
				_storage.CreateTree(tx, _indexTermsTreeName);
				tx.Commit();
            }
        }
    }
}
