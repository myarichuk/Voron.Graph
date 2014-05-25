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
	public class Etag : IEquatable<Etag>, IComparable<Etag>, IComparable
	{
		private static long _globalCount;
        private readonly static Etag _emptyInstance;
        private readonly static Etag _invalidInstance;

		private const int SizeOfLong = sizeof (long);

		private readonly long _timestamp;
		private readonly long _count;

        public const int Size = SizeOfLong * 2;

        static Etag()
        {
            _emptyInstance = new Etag(0, 0);
            _invalidInstance = new Etag(Int64.MinValue,Int64.MinValue);
        }

        public long Count
        {
            get
            {
                return _count;
            }
        }

        public long Timestamp
        {
            get
            {
                return _timestamp;
            }
        }

		public Etag(long count, long timestamp)
		{
			_count = count;
			_timestamp = timestamp;
		}		

        public Etag(byte[] etagBytes)
        {
            if (etagBytes == null || etagBytes.Length != Size)
                throw new ArgumentException("Invalid etag byte array");

            _timestamp = EndianBitConverter.Big.ToInt64(etagBytes, 0);
            _count = EndianBitConverter.Big.ToInt64(etagBytes, SizeOfLong);
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

        public Slice ToSlice()
        {
            var sliceWriter = new SliceWriter(SizeOfLong * 2);
            sliceWriter.WriteBigEndian(_timestamp);
            sliceWriter.WriteBigEndian(_count);
            
            return sliceWriter.CreateSlice();
        }

        public override string ToString()
        {
            return String.Format("{0}-{1}", _timestamp, _count);
        }

        public static Etag Empty
        {
            get
            {
                return _emptyInstance;
            }
        }
        
        public static Etag Invalid
        {
            get
            {
                return _invalidInstance;
            }
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
			return left._count > right._count && left._timestamp >= right._timestamp;
		}

		public static bool operator <(Etag left, Etag right)
		{
			return left._count < right._count && left._timestamp <= right._timestamp;
		}

		#endregion


        public int CompareTo(object otherObject)
        {
            if (otherObject == null)
                return -1;

            var otherEtag = otherObject as Etag;
            return CompareTo(otherEtag);
        }

        public int CompareTo(Etag otherEtag)
        {
            if (otherEtag == null)
                return -1;
            if (ReferenceEquals(this, otherEtag) || this == otherEtag)
                return 0;

            return this > otherEtag ? 1 : -1;
        }
    }
}
