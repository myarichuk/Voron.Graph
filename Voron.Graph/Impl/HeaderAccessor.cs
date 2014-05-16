using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public unsafe class HeaderAccessor : IDisposable
    {
        private readonly FileStream _headerFileStream;
        private const string HeaderFilename = "voron.graph.header";
        private bool isDisposed;
        private readonly int _headerSize;
        private readonly byte[] _tempBuffer;

        private readonly object _syncObject = new object();

        public HeaderAccessor()
        {
            _headerSize = Marshal.SizeOf(typeof(Header));
            _tempBuffer = new byte[_headerSize];
            isDisposed = false;
            var fileExistedBefore = File.Exists(HeaderFilename);
            _headerFileStream = new FileStream(HeaderFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (!fileExistedBefore)
                _headerFileStream.SetLength(_headerSize);
        }

        public void Modify(Func<Header,Header> modifyFunc)
        {
            var modifiedHeader = modifyFunc(GetHeader());
            Save(modifiedHeader);
        }

        public T Get<T>(Func<Header,T> fetchFunc)
        {
            var header = GetHeader();
            return fetchFunc(header);
        }

        private Header GetHeader()
        {
            _headerFileStream.Position = 0;
            lock (_syncObject)
            {
                _headerFileStream.Read(_tempBuffer, 0, _headerSize);
                fixed (byte* ptr = _tempBuffer)
                    return *((Header*)ptr);
            }
        }

        private void Save(Header header)
        {
            _headerFileStream.Position = 0;
            byte* headerPtr = (byte*)&header;
            lock (_syncObject)
            {
                fixed (byte* ptr = _tempBuffer)
                    NativeMethods.memcpy(ptr, headerPtr, _headerSize);
                _headerFileStream.Write(_tempBuffer, 0, _headerSize);
            }
        }

        public void Flush()
        {
            _headerFileStream.Flush(true);
        }

        public void Dispose()
        {
            if(_headerFileStream != null && !isDisposed)
            {
                _headerFileStream.Dispose();
                isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }

        ~HeaderAccessor()
        {
            if(!isDisposed)
                Dispose();
        }
    }
}
