using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public class Node : IDisposable
    {
        public string Key { get; set; }

        public Stream Data { get; private set; }

        public Node(string key, Stream data, bool makeDataCopy = true)
        {
            Key = key;
            if(makeDataCopy == true)
            {
                Data = new MemoryStream();
                data.CopyTo(Data);
                Data.Position = 0;
            }
            else
                Data = data;
        }

        public Node(string key, byte[] data)
        {
            Key = key;
            Data = (data != null) ? new MemoryStream(data) : Stream.Null;
        }

        public void Dispose()
        {
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
                
        }
    }
}
