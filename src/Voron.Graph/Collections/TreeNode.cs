using System.Collections.Generic;

namespace Voron.Graph.Collections
{
	public class TreeNode<T>
	{
		public TreeNode<T> Parent { get; set; }
		public ICollection<TreeNode<T>> Children { get; set; }

		public T Data { get; set; }
	}
}
