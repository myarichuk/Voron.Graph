using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Impl
{
    public class InMemoryHeaderAccessor : IHeaderAccessor
    {
        private Header _header;

        private readonly object _syncObject = new object();

        public InMemoryHeaderAccessor(Func<Header> initializer)
        {
            _header = initializer();
        }

        public void Modify(Func<Header, Header> modifyFunc)
        {
            lock (_syncObject)
            {
                _header = modifyFunc(_header);
            }
        }

        public T Get<T>(Func<Header, T> fetchFunc)
        {
            lock (_syncObject)
            {
                return fetchFunc(_header);
            }
        }

        public void Dispose()
        {
            
        }
    }
}
