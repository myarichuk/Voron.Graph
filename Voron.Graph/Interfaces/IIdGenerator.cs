using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Interfaces
{
    public interface IIdGenerator
    {
        long NextId();
    }
}
