using System.Runtime.InteropServices;

namespace Sparrow.Platform
{
    public static class Platform
    {
#if DNXCORE50
        public static readonly bool RunningOnPosix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
        //if not in DNX core env -> incompatible with unix anyway
        public static readonly bool RunningOnPosix = false;
#endif
    }
}