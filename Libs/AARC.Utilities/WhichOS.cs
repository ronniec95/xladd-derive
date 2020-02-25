using System.Runtime.InteropServices;

namespace AARC.Utilities
{
    public static class WhichOS
    {
        public static bool IsWindows =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
