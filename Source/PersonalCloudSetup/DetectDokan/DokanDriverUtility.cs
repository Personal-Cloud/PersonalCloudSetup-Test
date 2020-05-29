using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DetectDokan
{
    internal static class DokanDriverUtility
    {
        private const int DOKAN_MAJOR_API_VERSION = 1;
        private const string DOKAN_MAJOR_API_VERSION_STR = "1";

        private const string DOKAN_GLOBAL_DEVICE_NAME = @"\\.\Dokan_" + DOKAN_MAJOR_API_VERSION_STR;

        // CTL_CODE(FILE_DEVICE_UNKNOWN, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)
        private readonly static uint IOCTL_TEST = CTL_CODE(FILE_DEVICE_UNKNOWN, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS);
        private const uint FILE_DEVICE_UNKNOWN = 0x00000022;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;

        public static bool QueryVersion(out uint version)
        {
            IntPtr hDevice = CreateFile(DOKAN_GLOBAL_DEVICE_NAME,
                FileAccess.ReadWrite,
                FileShare.None, IntPtr.Zero, FileMode.Open,
                FileOptions.None, IntPtr.Zero);
            if (hDevice != (IntPtr)(-1))
            {
                try
                {
                    byte[] buffer = new byte[sizeof(uint)];
                    uint returnedDataSize = 0;
                    using (var ap = new AutoPinner(buffer))
                    {
                        if (DeviceIoControl(hDevice, IOCTL_TEST, IntPtr.Zero, 0, ap, (uint)buffer.Length, ref returnedDataSize, IntPtr.Zero))
                        {
                            if (returnedDataSize == sizeof(uint))
                            {
                                version = BitConverter.ToUInt32(buffer, 0);
                                return true;
                            }
                        }
                        else
                        {
                            int error = Marshal.GetLastWin32Error();
                        }
                    }
                }
                finally
                {
                    CloseHandle(hDevice);
                }
            }

            version = 0;
            return false;
        }

        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            // https://docs.microsoft.com/en-us/windows-hardware/drivers/kernel/defining-i-o-control-codes
            // #define CTL_CODE(DeviceType, Function, Method, Access)
            //         (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
            return (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method));
        }

        #region Native Methods

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile
            (
            string filename,
            FileAccess access,
            FileShare sharing,
            IntPtr SecurityAttributes,
            FileMode mode,
            FileOptions options,
            IntPtr template
            );

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl
            (
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint nBytesReturned,
            IntPtr lpOverlapped
            );

        [DllImport("kernel32.dll")]
        private static extern void CloseHandle(IntPtr handle);

        #endregion Native Methods
    }
}
