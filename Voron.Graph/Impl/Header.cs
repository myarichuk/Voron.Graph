using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Header
    {        
        public ushort Version;

        public long NextHi;
    }
}
