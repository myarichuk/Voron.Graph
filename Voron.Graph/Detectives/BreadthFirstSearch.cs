using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Util;

namespace Voron.Graph.Detectives
{
    public class BreadthFirstSearch : IGraphDetective
    {
        private readonly Session _session;
        private readonly string _nodeTreeName;
        private readonly string _edgeTreeName;
        private readonly string _nodesWithEdgesTreeName;
        private readonly HashSet<string> _visitedNodes;

        internal BreadthFirstSearch(Session session)
        {
            _session = session;
            _nodeTreeName = _session.NodeTreeName;
            _edgeTreeName = _session.EdgeTreeName;
            _nodesWithEdgesTreeName = _session.NodesWithEdgesTreeName;
            _visitedNodes = new HashSet<string>();
        }

        public Stream FindOne(Func<string, Stream,bool> predicate)
        {            
            var firstNodeKey = GetStartingKey();
            if(firstNodeKey == null)
                return null;
            var searchQueue = new Queue<string>();

            _visitedNodes.Add(firstNodeKey);
            searchQueue.Enqueue(firstNodeKey);
            while (searchQueue.Count > 0)
            {
                var currentKey = searchQueue.Dequeue();
                var readResult = _session.Snapshot.Read(_nodeTreeName, currentKey,_session.WriteBatch).Reader;
                if(readResult == null)
                    throw new InvalidDataException("Key fetched from 'nodes with edges' tree was not found in nodes tree. Data corruption?");

                var currentValue = readResult.AsStream();

                if (predicate(currentKey, currentValue))
                    return currentValue;

                var connectedNods = GetNodesConnectedTo(currentKey).ToList();
                foreach (var key in connectedNods)
                    searchQueue.Enqueue(key);
            }

            //now check disconnected nodes
            using (var nodesIterator = _session.Snapshot
                .Iterate(_session.DisconnectedNodesTreeName, _session.WriteBatch))
            {
                if (nodesIterator.Seek(Slice.BeforeAllKeys))
                {
                    do
                    {
                        var currentValue =
                            _session.Snapshot.Read(_nodeTreeName, nodesIterator.CurrentKey, _session.WriteBatch)
                                .Reader.AsStream();
                        var currentKey = nodesIterator.CurrentKey.ToString();
                        if (predicate(currentKey, currentValue))
                            return currentValue;
                    } while (nodesIterator.MoveNext());
                }
            }

            return null;
        }

        private IEnumerable<string> GetNodesConnectedTo(string nodeKey)
        {
            using (var edgeIterator = _session.Snapshot.MultiRead(_edgeTreeName, nodeKey))
            {
                if(!edgeIterator.Seek(Slice.BeforeAllKeys))
                    yield break;

                do
                {
                    var key = edgeIterator.CurrentKey.ToString();
                    if (!_visitedNodes.Contains(key))
                    {
                        _visitedNodes.Add(key);
                        yield return key;
                    }
                } while (edgeIterator.MoveNext());
            }
        }

        private string GetStartingKey()
        {
            string firstNodeKey;
            using (var nodeIterator = _session.Snapshot.Iterate(_nodesWithEdgesTreeName, _session.WriteBatch))
            {
                if (!nodeIterator.Seek(Slice.BeforeAllKeys))
                    return null;

                firstNodeKey = nodeIterator.CurrentKey.ToString();
            }
            return firstNodeKey;
        }


        public bool Contains(Func<string, Stream, bool> predicate)
        {
            return FindOne(predicate) != null;
        }
    }
}
