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

        public EdgeTreeKey Key { get; private set; }

        public Stream Data { get; private set; }

        public Edge(long nodeKeyFrom, long nodeKeyTo, Stream data = null, bool makeDataCopy = true)
        {
            Key = new EdgeTreeKey
            {
                NodeKeyFrom = nodeKeyFrom,
                NodeKeyTo = nodeKeyTo
            };
            
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

        public Edge(long keyFrom, long keyTo, byte[] data)
            : this(keyFrom,keyTo,(data != null) ? new MemoryStream(data) : Stream.Null, false)
        {
        }

        public void Dispose()
        {
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
        }

        ~Edge()
        {
            Dispose();
        }
    }
}
