//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Voron.Graph.Algorithms.Search;

//namespace Voron.Graph.Algorithms.ShortestPath
//{
//    public class DijkstraShortestPath : BaseAlgorithm
//    {
//        private TraversalAlgorithm _traversal;
//        private Dictionary<long, long> _distancesByNodeKey;
//        private Dictionary<long, long> _previousOptimalNodeKey;
        

//        public DijkstraShortestPath(GraphStorage graphStorage, Node root)
//        {
//            _traversal = new BreadthFirstSearch(graphStorage, cancelToken);
//            _distancesByNodeKey = new Dictionary<long, long>();
//            _previousOptimalNodeKey = new Dictionary<long, long>();

//            _distancesByNodeKey[root.Key] = 0;

//            _traversal.NodeVisited += visitedEventArgs =>
//            {
//                if(_distancesByNodeKey.ContainsKey(visitedEventArgs.VisitedNode.Key) == false)
//                {
//                    _distancesByNodeKey.Add(visitedEventArgs.VisitedNode.Key, long.MaxValue); //as if long.MaxValue = infinity
//                    _previousOptimalNodeKey.Add(visitedEventArgs.VisitedNode.Key, visitedEventArgs.PreviousNode.Key);
//                }
//                else
//                {
//                    var newWeight = _distancesByNodeKey[visitedEventArgs.VisitedNode.Key] + visitedEventArgs.Weight;
//                }
//            };
//        }

//        public Task<Results> Start()
//        {
//            throw new NotImplementedException();
//        }

//        protected override Node GetDefaultRootNode(Transaction tx)
//        {
//            using (var iter = tx.NodeTree.Iterate())
//            {
//                if (!iter.Seek(Slice.BeforeAllKeys))
//                    return null;

//                using (var resultStream = iter.CreateReaderForCurrent().AsStream())
//                {
//                    Etag etag;
//                    JObject value;
//                    Util.EtagAndValueFromStream(resultStream, out etag, out value);
//                    return new Node(iter.CurrentKey.CreateReader().ReadBigEndianInt64(), value, etag);
//                }
//            }
//        }

//        public class Results
//        {
//            public Dictionary<long, long> DistancesByNode { get; internal set; }
//            public Dictionary<long, long> PreviousNodeInOptimalPath { get; internal set; }
//        }
//    }
//}
