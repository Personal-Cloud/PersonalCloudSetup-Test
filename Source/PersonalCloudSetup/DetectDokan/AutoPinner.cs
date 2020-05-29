using System;
using System.Runtime.InteropServices;

namespace DetectDokan
{
    // https://stackoverflow.com/questions/537573/how-to-get-intptr-from-byte-in-c-sharp
    internal class AutoPinner : IDisposable
    {
        private GCHandle _PinnedArray;

        public AutoPinner(object obj)
        {
            _PinnedArray = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        public static implicit operator IntPtr(AutoPinner ap)
        {
            return ap._PinnedArray.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            _PinnedArray.Free();
        }
    }
}
