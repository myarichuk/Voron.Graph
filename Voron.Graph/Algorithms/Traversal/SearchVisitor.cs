using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Voron.Graph.Primitives;

namespace Voron.Graph.Algorithms.Traversal
{
    public class SearchVisitor : IVisitor
    {
        private readonly Func<JObject, bool> _searchPredicate;
        private readonly int _take;
        private readonly HashSet<Node> _results;

        /// <summary>
        /// Search Results
        /// </summary>
        public IEnumerable<Node> Results
        {
            get
            {
                return _results;
            }
        }

        public SearchVisitor(Func<JObject, bool> searchPredicate)
            :this(searchPredicate, 0)
        {
        }

        public SearchVisitor(Func<JObject, bool> searchPredicate, int take)
        {
            if (searchPredicate == null)
                throw new ArgumentNullException("searchPredicate");

            _searchPredicate = searchPredicate;
            _results = new HashSet<Node>();
            _take = take;
        }

        public void DiscoverAdjacent(NodeWithEdge neighboorNode)
        {
        }

        public void ExamineTraversalInfo(TraversalNodeInfo traversalNodeInfo)
        {
            if (_searchPredicate(traversalNodeInfo.CurrentNode.Data))
                _results.Add(traversalNodeInfo.CurrentNode);
        }

        public bool ShouldStopTraversal
        {
            get 
            {
                if (_take <= 0)
                    return false;
                else
                    return _results.Count >= _take;
            }
        }


        public bool ShouldSkip(TraversalNodeInfo traversalNodeInfo)
        {
            return false;
        }
    }
}
