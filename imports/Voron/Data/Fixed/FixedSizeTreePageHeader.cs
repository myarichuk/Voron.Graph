﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voron.Data.BTrees;

namespace Voron.Data.Fixed
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct FixedSizeTreePageHeader
    {
        [FieldOffset(0)]
        public long PageNumber;

        [FieldOffset(8)]
        public ushort StartPosition;

        [FieldOffset(10)]
        public ushort NumberOfEntries;

        [FieldOffset(12)]
        public PageFlags Flags;

        [FieldOffset(13)]
        public FixedSizeTreePageFlags TreeFlags;

        [FieldOffset(14)]
        public ushort ValueSize;
    }
}
