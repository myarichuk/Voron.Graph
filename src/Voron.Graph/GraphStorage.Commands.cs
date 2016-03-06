using Sparrow;
using System.IO;
using System.Runtime.CompilerServices;
using Voron.Data.Tables;

namespace Voron.Graph
{
    public unsafe partial class GraphStorage
    {
        private readonly Slice _key = new Slice(SliceOptions.Key);

		public long AddVertex(Transaction tx, byte[] data)
		{
			fixed(byte* dataPtr = data)
				return AddVertex(tx, dataPtr, data.Length);
		}

		public long AddVertex(Transaction tx, byte* data, int dataSize)
		{
			ThrowIfDisposed();

			var id = NextValue(tx, IncrementingValue.Id);
			var flippedBitsId = Util.ReverseBits(id);
			var etag = NextValue(tx, IncrementingValue.VertexEtag);
			//since we are single threaded, this should be ok
			var valueBuilder = new TableValueBuilder
			{
				{ (byte*)&flippedBitsId, sizeof(long) },
				{ (byte*)&etag, sizeof(long) },
				{ data, dataSize }
			};

			tx.VertexTable.Set(valueBuilder);
			return flippedBitsId;
		}

        //pointer is valid only if the transaction is valid
        public byte* ReadVertexData(Transaction tx, long id, out int size)
        {
            ThrowIfDisposed();
			_key.Set((byte*)&id, sizeof(long));
			var valueReader = tx.VertexTable.ReadByKey(_key);
			if (valueReader == null)
			{
				size = -1;
				return null;
			}

			return valueReader.Read((int)VertexTableFields.Data, out size);
        }

		public byte[] ReadVertexData(Transaction tx, long id)
		{
			ThrowIfDisposed();

			int size;
			var fetchedDataPtr = ReadVertexData(tx, id, out size);
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
			_key.Set((byte*)&id, sizeof(long));
			tx.VertexTable.DeleteByKey(_key);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long AddEdge(Transaction tx, long fromId, long toId)
		{
			return AddEdge(tx, fromId, toId, null, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long AddEdge(Transaction tx, long fromId, long toId, byte[] data)
		{
			fixed(byte* dataPtr = data)
				return AddEdge(tx, fromId, toId, dataPtr, data.Length);
		}

		public long AddEdge(Transaction tx, long fromId, long toId, byte* data, int size)
        {
            ThrowIfDisposed();
			var id = NextValue(tx, IncrementingValue.Id);
			var etag = NextValue(tx, IncrementingValue.EdgeEtag);
			var flippedBitsId = Util.ReverseBits(id);
			var val = new TableValueBuilder
			{
				{ (byte*)&flippedBitsId,sizeof(long) },
				{ (byte*)&etag,sizeof(long) },
				{ (byte*)&fromId,sizeof(long) },
				{ (byte*)&toId, sizeof(long) },
				{ data, size }
			};		

            tx.EdgesTable.Set(val);

			return flippedBitsId;
        }

		public byte[] ReadEdgeData(Transaction tx, long id)
		{
			int size;
			var fetchedDataPtr = ReadEdgeData(tx, id, out size);
			if (fetchedDataPtr == (byte*)0)
				return null;

			var data = new byte[size];
			fixed (byte* dataPtr = data)
				Memory.Copy(dataPtr, fetchedDataPtr, size);

			return data;

		}

		public byte* ReadEdgeData(Transaction tx, long id, out int size)
		{
			ThrowIfDisposed();
			_key.Set((byte*)&id, sizeof(long));
			var valueReader = tx.EdgesTable.ReadByKey(_key);
			if (valueReader == null)
			{
				size = -1;
				return null;
			}

			return valueReader.Read((int)EdgeTableFields.Data, out size);
		}
    }
}
