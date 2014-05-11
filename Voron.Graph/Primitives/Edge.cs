using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public class Edge : IDisposable
    {
        public string KeyFrom { get; set; }

        public string KeyTo { get; set; }

        public Stream Data { get; private set; }

        public Edge(string keyFrom, string keyTo, Stream data = null, bool makeDataCopy = true)
        {
            KeyFrom = keyFrom;
            KeyTo = keyTo;
            if (data == null)
                Data = Stream.Null;
            else if (makeDataCopy)
            {
                Data = new BufferedStream(new MemoryStream());
                data.CopyTo(Data);
                Data.Position = 0;
            }
            else
                Data = data;
        }

        public Edge(string keyFrom, string keyTo, byte[] data)
        {
            KeyFrom = keyFrom;
            KeyTo = keyTo;

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
