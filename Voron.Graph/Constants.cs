using System.IO;
namespace Voron.Graph
{
    public static class Constants
    {
        public const int Version = 1;

        public const string EdgeTreeNameSuffix = "_EdgeTree";
        public const string NodesWithEdgesTreeNameSuffix = "_NodeWithEdgesTree";
        public const string NodeTreeNameSuffix = "_NodeTree";
        public const string DisconnectedNodesTreeNameSuffix = "_DisconnectedNodes";
        public const string KeyByEtagTreeNameSuffix = "_EtagByKeyTree";

        public const string GraphMetadataKeySuffix = "_GraphMetadata";

        public const string IndexedPropertyListKey = "IndexedProperties_SystemMetadata";
		public readonly static string DataFolder = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Data";

    }
}
