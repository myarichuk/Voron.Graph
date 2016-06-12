using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sparrow.Binary
{
    public static class Bits
    {
        // Code taken from http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn

        private static readonly int[] MultiplyDeBruijnBitPosition = 
                {
                    0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
                    8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
                };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MostSignificantBit(uint n)
        {
            n |= n >> 1; // first round down to one less than a power of 2 
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return MultiplyDeBruijnBitPosition[(uint)(n * 0x07C4ACDDU) >> 27];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MostSignificantBit(int n)
        {
            n |= n >> 1; // first round down to one less than a power of 2 
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return MultiplyDeBruijnBitPosition[(uint)(n * 0x07C4ACDDU) >> 27];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MostSignificantBit(long nn)
        {
            unchecked
            {                
                if (nn == 0) return 0;

                ulong n = (ulong) nn;
                int msb = 0;

                if ((n & 0xFFFFFFFF00000000L) != 0)
                {
                    n >>= (1 << 5);
                    msb += (1 << 5);
                }

                if ((n & 0xFFFF0000) != 0)
                {
                    n >>= (1 << 4);
                    msb += (1 << 4);
                }

                // Now we find the most significant bit in a 16-bit word.

                n |= n << 16;
                n |= n << 32;

                ulong y = n & 0xFF00F0F0CCCCAAAAL;

                ulong t = 0x8000800080008000L & (y | ((y | 0x8000800080008000L) - (n ^ y)));

                t |= t << 15;
                t |= t << 30;
                t |= t << 60;

                return (int)((ulong)msb + (t >> 60));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MostSignificantBit(ulong n)
        {
            if ( n == 0 ) return 0;
        
            ulong msb = 0;
        
            if ( ( n & 0xFFFFFFFF00000000L ) != 0 ) {
                n >>= ( 1 << 5 );
                msb += ( 1 << 5 );
            }
        
            if ( ( n & 0xFFFF0000 ) != 0 ) {
                n >>= ( 1 << 4 );
                msb += ( 1 << 4 );
            }
        
            // Now we find the most significant bit in a 16-bit word.
        
            n |= n << 16;
            n |= n << 32;
        
            ulong y = n & 0xFF00F0F0CCCCAAAAL;
        
            ulong t = 0x8000800080008000L & ( y | (( y | 0x8000800080008000L ) - ( n ^ y )));
        
            t |= t << 15;
            t |= t << 30;
            t |= t << 60;
        
            return (int)( msb + ( t >> 60 ) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeroes(int n)
        {
            if (n == 0)
                return 32;
            return 31 - MostSignificantBit(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeroes(uint n)
        {
            if (n == 0)
                return 32;
            return 31 - MostSignificantBit(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilLog2(int n)
        {
            int v = n;
            v |= v >> 1; // first round down to one less than a power of 2 
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;

            int pos = MultiplyDeBruijnBitPosition[(uint)(v * 0x07C4ACDDU) >> 27];
            if (n > (v & ~(v >> 1)))
                return pos + 1;
            else
                return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilLog2(uint n)
        {
            uint v = n;
            v |= v >> 1; // first round down to one less than a power of 2 
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;

            int pos = MultiplyDeBruijnBitPosition[(uint)(v * 0x07C4ACDDU) >> 27];
            if (n > (v & ~(v >> 1)))
                return pos + 1;
            else
                return pos;
        }

        private static readonly int[] nextPowerOf2Table =
        {
              0,   1,   2,   4,   4,   8,   8,   8,   8,  16,  16,  16,  16,  16,  16,  16, 
             16,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,  32,
             32,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64, 
             64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64,  64, 
             64, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 
            128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 
            128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 
            128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 
            128, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 
            256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOf2(int v)
        {
            if (v < nextPowerOf2Table.Length)
            {
                return nextPowerOf2Table[v];
            }
            else
            {
                return NextPowerOf2Internal(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NextPowerOf2(long v)
        {
            if (v < nextPowerOf2Table.Length)
            {
                return nextPowerOf2Table[v];
            }
            else
            {
                return NextPowerOf2Internal(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextPowerOf2Internal(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long NextPowerOf2Internal(long v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            v++;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft32(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateRight32(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateLeft64(ulong value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateRight64(ulong value, int count)
        {
            return (value >> count) | (value << (64 - count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SwapBytes(uint value)
        {
            return (((value & 0xff000000) >> 24) |
                    ((value & 0x00ff0000) >> 8)  |
                    ((value & 0x0000ff00) << 8)  |
                    ((value & 0x000000ff) << 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SwapBytes(long value)
        {
            return (long)SwapBytes((ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SwapBytes(ulong value)
        {
            return (((value & 0xff00000000000000UL) >> 56) |
                    ((value & 0x00ff000000000000UL) >> 40) |
                    ((value & 0x0000ff0000000000UL) >> 24) |
                    ((value & 0x000000ff00000000UL) >> 8) |
                    ((value & 0x00000000ff000000UL) << 8) |
                    ((value & 0x0000000000ff0000UL) << 24) |
                    ((value & 0x000000000000ff00UL) << 40) |
                    ((value & 0x00000000000000ffUL) << 56));
        }
    }
}
