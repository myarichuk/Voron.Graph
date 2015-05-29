using System.Collections.Concurrent;
using System.ServiceModel.Channels;

namespace Voron.Graph.Misc
{
	/// <summary>
	/// Simple class to reuse small buffer arrays allocated for conversions
	/// </summary>
	public static class BufferPool
	{
		private static readonly ConcurrentDictionary<int, ConcurrentQueue<byte[]>> _bufferPool;

		static BufferPool()
		{
			_bufferPool = new ConcurrentDictionary<int, ConcurrentQueue<byte[]>>();
        }	

		public static byte[] TakeBuffer(int size)
		{
			var pool = _bufferPool.GetOrAdd(size, _ => new ConcurrentQueue<byte[]>());

			byte[] buffer;
			if (pool.TryDequeue(out buffer))
				return buffer;

			return new byte[size];
		}

		public static void ReturnBuffer(byte[] buffer)
		{
			var pool = _bufferPool.GetOrAdd(buffer.Length, _ => new ConcurrentQueue<byte[]>());
			pool.Enqueue(buffer);
		}
	}
}