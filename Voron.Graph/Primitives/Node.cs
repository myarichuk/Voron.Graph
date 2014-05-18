using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public class Node
    {
        public long Key { get; set; }

        public JObject Data { get; private set; }

        public Node(long key, object data)
            :this(key, JObject.FromObject(data))
        {       
        }

        public Node(long key, JObject data)
        {
            Key = key;
            Data = data;
        }
    }
}
