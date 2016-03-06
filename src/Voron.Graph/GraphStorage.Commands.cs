using Sparrow;
using System.IO;
using Voron.Data.Tables;

namespace Voron.Graph
{
    public unsafe partial class GraphStorage
    {
        private readonly Slice _currentKey = new Slice(SliceOptions.Key);

		public long AddVertex(Transaction tx, byte[] data)
		{
			fixed(byte* dataPtr = data)
				return AddVertex(tx, dataPtr, data.Length);
		}

		public long AddVertex(Transaction tx, byte* data, int dataSize)
		{
			ThrowIfDisposed();

			var id = NextValue(tx, IncrementingValue.VertexId);
			var flippedBitsId = Util.ReverseBits(id);
			var etag = NextValue(tx, IncrementingValue.VertexEtag);
			//since we are single threaded, this should be ok
			var valueBuilder = new TableValueBuilder();

			valueBuilder.Add((byte*)&flippedBitsId, sizeof(long));
			valueBuilder.Add((byte*)&etag, sizeof(long));
			valueBuilder.Add(data, dataSize);
			tx.VertexTable.Set(valueBuilder);
			return flippedBitsId;
		}

        //pointer is valid only if the transaction is valid
        public byte* ReadVertex(Transaction tx, long id, out int size)
        {
            ThrowIfDisposed();
			_currentKey.Set((byte*)&id, sizeof(long));
			var valueReader = tx.VertexTable.ReadByKey(_currentKey);
			if (valueReader == null)
			{
				size = -1;
				return null;
			}

			return valueReader.Read((int)VertexTableFields.Data, out size);
        }

		public byte[] ReadVertex(Transaction tx, long id)
		{
			ThrowIfDisposed();

			int size;
			var fetchedDataPtr = ReadVertex(tx, id, out size);
			if (fetchedDataPtr == (byte*)0)
				return null;

			var data = new byte[size];
			fixed(byte* dataPtr = data)
				Memory.Copy(dataPtr, fetchedDataPtr, size);

			return data;
		}

		public void DeleteVertex(Transaction tx, long id)
        {
            ThrowIfDisposed();
			_currentKey.Set((byte*)&id, sizeof(long));
			tx.VertexTable.DeleteByKey(_currentKey);
        }

        public void AddEdge(Transaction tx, long fromId, long toId)
        {
            ThrowIfDisposed();
            var val = new TableValueBuilder();
            val.Add((byte*)&fromId,sizeof(long));
            val.Add((byte*)&toId, sizeof(long));
            tx.AdjacencyListTable.Set(val);
        }
    }
}
