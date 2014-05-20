using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public static class Constants
    {
        public const int Version = 1;

        public const string EdgeTreeNameSuffix = "_EdgeTree";
        public const string NodesWithEdgesTreeNameSuffix = "_NodeWithEdgesTree";
        public const string NodeTreeNameSuffix = "_NodeTree";
        public const string DisconnectedNodesTreeName = "_DisconnectedNodes";

        public const int WriteSequenceCreationDefaultTimeoutInMs = 30000;
    }
}
