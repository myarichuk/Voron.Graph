using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Interfaces
{
    public interface IHeaderAccessor : IDisposable
    {
        void Modify(Func<Header, Header> modifyFunc);

        T Get<T>(Func<Header, T> fetchFunc);
    }
}
