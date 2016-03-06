using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public unsafe partial class GraphStorage
    {
        public long AddVertex(Transaction tx, Stream data)
        {
            ThrowIfDisposed();

            var id = NextVertexId(tx.VoronTx);
            var key = new Slice((byte*)&id, sizeof(long));

            tx.VertexTree.Add(key,data);
            return id;
        }

        //note - stream retreived from this function
        //is valid only if the transaction is valid
        public Stream ReadVertex(Transaction tx, long id)
        {
            ThrowIfDisposed();

            var key = new Slice((byte*)&id, sizeof(long));
            var res = tx.VertexTree.Read(key);
            return res == null ? null : res.Reader.AsStream();
        }

        public void DeleteVertex(Transaction tx, long id)
        {
            ThrowIfDisposed();

            var key = new Slice((byte*)&id, sizeof(long));
            tx.VertexTree.Delete(key);
        }
    }
}
