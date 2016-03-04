using System;
using System.Threading.Tasks;
using Voron.Impl.Paging;

namespace Voron.Impl.Journal
{
    public unsafe interface IJournalWriter : IDisposable
    {
        void WriteGather(long position, IntPtr[] pages);
        long NumberOfAllocatedPages { get;  }
        bool Disposed { get; }
        bool DeleteOnClose { get; set; }
        IVirtualPager CreatePager();
        bool Read(long pageNumber, byte* buffer, int count);
    }
}
