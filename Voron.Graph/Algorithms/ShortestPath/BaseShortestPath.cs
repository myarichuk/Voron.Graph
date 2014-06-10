using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Algorithms.Traversal;

namespace Voron.Graph.Algorithms.ShortestPath
{
    public abstract class BaseShortestPath : BaseAlgorithm, ISingleSourceShortestPath
    {
        protected readonly IVisitor _visitor;
        protected readonly TraversalAlgorithm _traversal;
        protected readonly Node _rootNode;

        protected BaseShortestPath(Transaction tx,
            GraphStorage graphStorage, 
            Node root, 
            CancellationToken cancelToken,
            IVisitor visitor,
            INodeTraversalStore<TraversalNodeInfo> processingQueue)
        {
            _rootNode = root;
            _visitor = visitor;
            _traversal = new TraversalAlgorithm(tx, graphStorage, root,processingQueue, cancelToken)
            {
                Visitor = visitor
            };
        }

        public ISingleSourceShortestPathResults Execute()
        {
            throw new NotImplementedException();
        }

        public Task<ISingleSourceShortestPathResults> ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
