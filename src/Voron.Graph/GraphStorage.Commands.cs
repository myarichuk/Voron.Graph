using Sparrow;
using System.Runtime.CompilerServices;
using Voron.Data.Tables;
using System.Linq;
using System.Collections.Generic;

namespace Voron.Graph
{
	public unsafe partial class GraphStorage
	{
		public long AddVertex(Transaction tx, byte[] data)
		{
			fixed(byte* dataPtr = data)
				return AddVertex(tx, dataPtr, data.Length);
		}

		public long AddVertex(Transaction tx, byte* data, int dataSize)
		{
			ThrowIfDisposed();

			var etag = NextValue(tx, IncrementingValue.VertexEtag);
			//since we are single threaded, this should be ok
			var valueBuilder = new TableValueBuilder
			{
				{ (byte*)&etag, sizeof(long) },
				{ data, dataSize }
			};

			return tx.VertexTable.Set(valueBuilder);
		}

		//pointer is valid only if the transaction is valid
		public byte* ReadVertexData(Transaction tx, long id, out int size)
		{
			ThrowIfDisposed();			

			int readerSize;
			//if already deleted, do not throw -> return null pointer
			var ptr = tx.VertexTable.DirectRead(id, out readerSize);
			if (ptr == null) 
			{
				size = -1;
				return null;
			}

			var valueReader = new TableValueReader(ptr, readerSize);
			return valueReader.Read((int)VertexTableFields.Data, out size);
		}

		public IReadOnlyList<byte> ReadVertexData(Transaction tx, long id)
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

		public void RemoveVertex(Transaction tx, long id)
		{
			ThrowIfDisposed();
			tx.VertexTable.Delete(id);
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

		public long AddEdge(Transaction tx, 
			long fromId, 
			long toId, 
			byte* data, 
			int size)
		{
			ThrowIfDisposed();
			var etag = NextValue(tx, IncrementingValue.EdgeEtag);

			var val = new TableValueBuilder
			{
				{ (byte*)&etag,sizeof(long) },
				{ (byte*)&fromId,sizeof(long) },
				{ (byte*)&toId, sizeof(long) },
				{ data, size }
			};		

			return tx.EdgeTable.Set(val);
		}

		public void RemoveEdge(Transaction tx, long id)
		{
			ThrowIfDisposed();
			tx.EdgeTable.Delete(id);
		}

		public IReadOnlyList<byte> ReadEdgeData(Transaction tx, long id)
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
			int readerSize;
			var ptr = tx.EdgeTable.DirectRead(id, out readerSize);
			if (ptr == null)
			{
				size = -1;
				return null;
			}

			var valueReader = new TableValueReader(ptr, readerSize);
			return valueReader.Read((int)EdgeTableFields.Data, out size);
		}

		public IReadOnlyList<long> GetAdjacent(Transaction tx, long fromId)
		{
			ThrowIfDisposed();

			ByteString seekKey;
			seekKey = _byteStringContext.FromPtr((byte*)&fromId, sizeof(long));

			try
			{
				var result = tx.EdgeTable.SeekForwardFrom(
					FromToIndex,
					new Slice(seekKey), true);


				return result.SelectMany(x =>
					x.Results.Select(r =>
					{
						int _;
						return *(long*)r.Read((int)EdgeTableFields.ToKey, out _);
					}))
					.ToList();
			}
			finally
			{
				_byteStringContext.Release(ref seekKey);
			}			
		}	
	}
}
