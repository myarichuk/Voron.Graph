using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    public interface IGraphAmender
    {
        void AcceptUpdateVisitor(IUpdateVisitor visitor);
    }
}
