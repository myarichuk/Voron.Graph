namespace Voron.Graph.Algorithms.Traversal
{
	//for each node during traversal holds information relevant to the process
	public class TraversalNodeInfo
    {
        public Node CurrentNode { get; set; }

        public Node ParentNode { get; set; }

        public int TraversalDepth { get; set; }

        public short LastEdgeWeight { get; set; }

        public int TotalEdgeWeightUpToNow { get; set; }
    }
}
