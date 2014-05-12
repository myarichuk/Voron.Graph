using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Trees;

namespace Voron.Graph
{
    public class Iterator<T> : IDisposable
        where T: class
    {
        private readonly IIterator _iterator;
        private readonly Func<Slice, Stream, T> _createItemFunc;

        internal Iterator(IIterator iterator, Func<Slice, Stream, T> createItemFunc)
        {
            _iterator = iterator;
            _createItemFunc = createItemFunc;
        }

        public T Current
        {
            get
            {
                var reader = _iterator.CreateReaderForCurrent();
                return _createItemFunc(_iterator.CurrentKey, reader.AsStream());
            }
        }

        /// <summary>
        /// seek to beginning of iterator
        /// </summary>
        /// <returns>returns false if iterator is empty, true otherwise</returns>
        public bool TrySeekToBegin()
        {
            return _iterator.Seek(Slice.BeforeAllKeys);
        }

        public bool MoveNext()
        {
            return _iterator.MoveNext();
        }      

        public void Dispose()
        {
            if (_iterator != null)
                _iterator.Dispose();
        }
    }
}
