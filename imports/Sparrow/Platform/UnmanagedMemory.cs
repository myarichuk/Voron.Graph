﻿using System;
using System.Runtime.InteropServices;

namespace Sparrow.Platform
{
    public unsafe static class UnmanagedMemory
    {
        public static IntPtr Copy(byte* dest, byte* src, int count)
        {
            return Platform.RunningOnPosix
                ? PosixUnmanagedMemory.Copy(dest, src, count)
                : Win32UnmanagedMemory.Copy(dest, src, count);
        }

        public static int Compare(byte* b1, byte* b2, int count)
        {
            return Platform.RunningOnPosix
                ? PosixUnmanagedMemory.Compare(b1, b2, count)
                : Win32UnmanagedMemory.Compare(b1, b2, count);
        }

        public static int Move(byte* b1, byte* b2, int count)
        {
            return Platform.RunningOnPosix
                ? PosixUnmanagedMemory.Move(b1, b2, count)
                : Win32UnmanagedMemory.Move(b1, b2, count);
        }

        public static IntPtr Set(byte* dest, int c, int count)
        {
            return Platform.RunningOnPosix
                ? PosixUnmanagedMemory.Set(dest, c, count)
                : Win32UnmanagedMemory.Set(dest, c, count);
        }
    }
}
