using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public partial class GraphStorage
    {
        public long AddVertex(Transaction tx, Stream data)
        {
			ThrowIfDisposed();

			var id = NextVertexId(tx.VoronTx);
			var key = id.ToSlice();

			tx.VertexTree.Add(key,data);
            return id;
        }

		//note - stream retreived from this function
		//is valid only if the transaction is valid
		public Stream ReadVertex(Transaction tx, long id)
		{
			ThrowIfDisposed();

			var key = id.ToSlice();
			var res = tx.VertexTree.Read(key);
			return res == null ? Stream.Null : res.Reader.AsStream();
		}
	}
}
