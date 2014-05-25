using Newtonsoft.Json.Linq;

namespace Voron.Graph
{
    public class Node
    {
        public long Key { get; set; }

        public JObject Data { get; private set; }

        public Etag Etag { get; internal set; }

        public Node(long key, JObject data, Etag etag = null)
        {
            Key = key;
            Data = data;
            Etag = etag ?? Etag.Empty;
        }
    }
}
