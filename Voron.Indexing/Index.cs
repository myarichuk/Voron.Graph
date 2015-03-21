using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Voron.Impl;
using Voron.Indexing.Tokenizers;

namespace Voron.Indexing
{
	public class Index
	{
		private readonly StorageEnvironment _env;
		private readonly HashSet<string> _indexedFields;
		private readonly string _indexMetadataTreeName;
		private readonly string _termsTreeName;
		private readonly string _termPositionsTreeName;

		private readonly BaseTokenizer _tokenizer;

		private const string IndexedFieldsKey = "indexed-fields";

		public Index(string name, StorageEnvironment env, BaseTokenizer tokenizer)
		{
			if (env == null) throw new ArgumentNullException("env");
			if (tokenizer == null) throw new ArgumentNullException("tokenizer");

			_env = env;
			_tokenizer = tokenizer;

			_indexMetadataTreeName = string.Format("{0}_index_metadata", name);
			_termsTreeName = string.Format("{0}_index_terms", name);
			_termPositionsTreeName = string.Format("{0}_index_term_positions", name);

			CreateSchema();
			_indexedFields = ReadIndexedFields();
		}

		public IEnumerable<string> IndexedFields
		{
			get { return _indexedFields; }
		}

		public void AddIndexedField(string fieldName)
		{
			var fieldCountBefore = _indexedFields.Count;
			_indexedFields.Add(fieldName);
			if (fieldCountBefore == _indexedFields.Count) 
				return;

			var writeBatch = new WriteBatch();
			writeBatch.MultiAdd(IndexedFieldsKey,fieldName,_indexMetadataTreeName);
			_env.Writer.Write(writeBatch);
		}

		public void RemoveIndexedField(string fieldName)
		{
			var fieldCountBefore = _indexedFields.Count;
			_indexedFields.Remove(fieldName);
			if (fieldCountBefore == _indexedFields.Count)
				return;

			var writeBatch = new WriteBatch();
			writeBatch.MultiDelete(IndexedFieldsKey, fieldName, _indexMetadataTreeName);
			_env.Writer.Write(writeBatch);
		}

		public void IndexIfRelevant(JObject data)
		{
			
		}

		private HashSet<string> ReadIndexedFields()
		{
			var indexedFields = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			using (var snapshot = _env.CreateSnapshot())
			using (var fieldNameIterator = snapshot.MultiRead(_indexMetadataTreeName, IndexedFieldsKey))
			{
				if (fieldNameIterator.Seek(Slice.BeforeAllKeys))
				{
					do
					{
						indexedFields.Add(fieldNameIterator.CurrentKey.ToString());
					} while (fieldNameIterator.MoveNext());
				}
			}

			return indexedFields;
		}	

		private void CreateSchema()
		{
			using (var tx = _env.NewTransaction(TransactionFlags.ReadWrite))
			{
				_env.CreateTree(tx, _termsTreeName);
				_env.CreateTree(tx, _termPositionsTreeName);
				_env.CreateTree(tx, _indexMetadataTreeName);

				tx.Commit();
			}
		}
	}
}
