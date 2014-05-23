using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Util.Conversion;

namespace Voron.Graph
{
	public class Etag : IEquatable<Etag>
	{
		private static long _globalCount;
		private const int SizeOfLong = sizeof (long);

		private readonly long _timestamp;
		private readonly long _count;

		public Etag(long count, long timestamp)
		{
			_count = count;
			_timestamp = timestamp;
		}		

		public static Etag Generate()
		{
			var count = Interlocked.Increment(ref _globalCount);
			var timestamp = DateTime.UtcNow.Ticks;

			return new Etag(count,timestamp);
		}

		public byte[] ToBytes()
		{
			var bytes = new byte[SizeOfLong*2];
			EndianBitConverter.Big.CopyBytes(_timestamp,bytes,0);
			EndianBitConverter.Big.CopyBytes(_count,bytes,SizeOfLong);

			return bytes;
		}

		public Stream ToStream()
		{
			return new MemoryStream(ToBytes());
		}

		#region Comparison Methods Implementation

		public bool Equals(Etag other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return _timestamp == other._timestamp && _count == other._count;
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			if (other.GetType() != GetType()) return false;
			return Equals((Etag) other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (_timestamp.GetHashCode()*397) ^ _count.GetHashCode();
			}
		}

		#endregion

		#region Comparison Operators Implementation

		public static bool operator ==(Etag left, Etag right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Etag left, Etag right)
		{
			return !Equals(left, right);
		}

		public static bool operator >(Etag left, Etag right)
		{
			return left._count > right._count && left._timestamp > right._timestamp;
		}

		public static bool operator <(Etag left, Etag right)
		{
			return left._count < right._count && left._timestamp < right._timestamp;
		}

		public static bool operator >=(Etag left, Etag right)
		{
			return left._count >= right._count && left._timestamp >= right._timestamp;
		}

		public static bool operator <=(Etag left, Etag right)
		{
			return left._count <= right._count && left._timestamp <= right._timestamp;
		}

		#endregion

	}
}
